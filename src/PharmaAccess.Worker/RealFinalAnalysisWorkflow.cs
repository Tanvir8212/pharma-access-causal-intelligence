using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PharmaAccess.Application.Causal;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Causal;
using PharmaAccess.Domain.Causal;
using PharmaAccess.ML;

namespace PharmaAccess.Worker;

internal sealed class RealFinalAnalysisWorkflow
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<(TrainNextQuarterStateEntryResult Predictive, RunCausalStudyResult Causal)> RunAsync(string connectionString, string artifactRoot)
    {
        var root = Path.GetFullPath(artifactRoot); Directory.CreateDirectory(root);
        await using var connection = new SqlConnection(connectionString); await connection.OpenAsync();
        var datasetId = await Scalar<int>(connection, "SELECT DatasetVersionId FROM core.DatasetVersion WHERE VersionCode='real-2021-2025-v1' AND Status='Validating' AND ValidationStatus='InProgress' AND FinalizedAtUtc IS NULL");
        var rows = await LoadRows(connection);
        var trainingRows = rows.Select(ToTraining).ToArray();
        var trainer = new NextQuarterStateEntryTrainingService(new FileSystemModelArtifactStore(Path.Combine(root, "models")));
        var predictive = await trainer.TrainAsync(new("real-next-quarter-entry-v1", datasetId, 1, NextQuarterFeatureSchema.Version,
            new(2022, 2023, 2023, 2024, 2024), [TrainerKind.LogisticRegression, TrainerKind.FastTree, TrainerKind.FastForest, TrainerKind.LightGbm],
            17, .5, true, false, "final-real-predictive", 250_000, TimeSpan.FromMinutes(30)), trainingRows);
        WritePredictive(root, predictive);

        var causalRows = rows.Select(ToCausal).ToArray();
        var causal = await new CausalStudyRunner().RunAsync(new(1, datasetId, 1, "neighbor-v1", "next-entry-v1", "peer-exposure-dag-v1", "peer-exposure-adjustment-v1",
            EstimandType.AverageTreatmentEffectOnTreated,
            [CausalEstimatorKind.AugmentedInverseProbabilityWeighting, CausalEstimatorKind.PropensityScoreWeighting, CausalEstimatorKind.OutcomeRegression, CausalEstimatorKind.UnadjustedDifferenceInMeans],
            20211, 20254, MissingDataPolicy.CompleteCase, new(500, .05, .02, .98, 17), new(10, false), new(50, 17, 20, .95), new(.1, 20, 10, 10), false, "final-real-causal", "Tanvir Mahmud Khan"), causalRows);
        WriteCausal(root, causal);
        WriteShared(root, rows, predictive, causal);
        return (predictive, causal);
    }

    public async Task<RunCausalStudyResult> RunCausalOnlyAsync(string connectionString, string artifactRoot)
    {
        var root=Path.GetFullPath(artifactRoot);Directory.CreateDirectory(root);await using var connection=new SqlConnection(connectionString);await connection.OpenAsync();
        var datasetId=await Scalar<int>(connection,"SELECT DatasetVersionId FROM core.DatasetVersion WHERE VersionCode='real-2021-2025-v1' AND Status='Validating' AND ValidationStatus='InProgress' AND FinalizedAtUtc IS NULL");
        var rows=await LoadRows(connection);var causal=await RunCausal(datasetId,rows);WriteCausal(root,causal);return causal;
    }

    public async Task<int> ExportCausalValidationRowsAsync(string connectionString,string outputPath)
    {
        await using var connection=new SqlConnection(connectionString);await connection.OpenAsync();var rows=await LoadRows(connection);var eligible=rows.Where(x=>!x.HasEntered&&!x.Censored&&x.Outcome.HasValue&&x.Treatment.HasValue&&x.NeighborShare.HasValue&&x.Peers>=2&&x.PrevND.HasValue&&x.PrevWD.HasValue&&x.PrevGap.HasValue&&x.Lag1Rx.HasValue).ToArray();
        var b=new StringBuilder("feature_row_id,generic_launch_id,state_id,approval_year,quarter,treatment,outcome,exposure,quarter_since_approval,state_weight,previous_nd,previous_wd,previous_gap,lag1_prescription_log\n");foreach(var x in eligible)b.AppendLine($"{x.Id},{x.LaunchId},{x.StateId},{x.ApprovalYear},{x.Quarter},{(x.Treatment==true?1:0)},{(x.Outcome==true?1:0)},{x.NeighborShare!.Value.ToString(CultureInfo.InvariantCulture)},{x.SinceApproval},{x.Weight.ToString(CultureInfo.InvariantCulture)},{x.PrevND!.Value.ToString(CultureInfo.InvariantCulture)},{x.PrevWD!.Value.ToString(CultureInfo.InvariantCulture)},{x.PrevGap!.Value.ToString(CultureInfo.InvariantCulture)},{Math.Log(1+x.Lag1Rx!.Value).ToString(CultureInfo.InvariantCulture)}");File.WriteAllText(Path.GetFullPath(outputPath),b.ToString());return eligible.Length;
    }

    private static Task<RunCausalStudyResult> RunCausal(int datasetId,AnalysisRow[] rows) => new CausalStudyRunner().RunAsync(new(1,datasetId,1,"neighbor-v1","next-entry-v1","peer-exposure-dag-v1","peer-exposure-adjustment-v1",EstimandType.AverageTreatmentEffectOnTreated,[CausalEstimatorKind.AugmentedInverseProbabilityWeighting,CausalEstimatorKind.PropensityScoreWeighting,CausalEstimatorKind.OutcomeRegression,CausalEstimatorKind.UnadjustedDifferenceInMeans],20211,20254,MissingDataPolicy.CompleteCase,new(100,.05,.02,.98,17),new(10,false),new(20,17,10,.95),new(.1,20,10,10),false,"final-real-causal","Tanvir Mahmud Khan"),rows.Select(ToCausal).ToArray());

    private static async Task<T> Scalar<T>(SqlConnection c, string sql) { await using var x = new SqlCommand(sql, c) { CommandTimeout = 120 }; return (T)Convert.ChangeType((await x.ExecuteScalarAsync())!, typeof(T), CultureInfo.InvariantCulture); }

    private static async Task<AnalysisRow[]> LoadRows(SqlConnection c)
    {
        const string sql = """
WITH X AS(
 SELECT p.*,al.ApprovalPageYear,al.Partition,s.StateId,w.PrescriptionNumerator,w.NormalizedWeight,
  LAG(p.PrescriptionCount,1) OVER(PARTITION BY p.AndaLaunchId,p.StateCode ORDER BY p.ObservationQuarter) Lag1Rx,
  LAG(p.PrescriptionCount,2) OVER(PARTITION BY p.AndaLaunchId,p.StateCode ORDER BY p.ObservationQuarter) Lag2Rx,
  LAG(p.ReimbursementAmount,1) OVER(PARTITION BY p.AndaLaunchId,p.StateCode ORDER BY p.ObservationQuarter) Lag1Reimb,
  LAG(p.ReimbursementAmount,2) OVER(PARTITION BY p.AndaLaunchId,p.StateCode ORDER BY p.ObservationQuarter) Lag2Reimb,
  LAG(p.NumericDistribution,51) OVER(PARTITION BY p.AndaLaunchId ORDER BY p.ObservationQuarter,p.StateCode) PrevND,
  LAG(p.WeightedDistribution,51) OVER(PARTITION BY p.AndaLaunchId ORDER BY p.ObservationQuarter,p.StateCode) PrevWD,
  LAG(p.AccessGap,51) OVER(PARTITION BY p.AndaLaunchId ORDER BY p.ObservationQuarter,p.StateCode) PrevGap
 FROM research.AndaStateQuarterPanel p JOIN research.AndaLaunch al ON al.AndaLaunchId=p.AndaLaunchId
 JOIN core.State s ON s.StateCode=p.StateCode JOIN research.HistoricalMarketWeight w ON w.StateCode=p.StateCode)
SELECT PanelRowId,AndaLaunchId,StateId,StateCode,Region,ApprovalPageYear,Partition,ObservationQuarter,QuarterSinceApproval,
 PrescriptionCount,ReimbursementAmount,Lag1Rx,Lag2Rx,Lag1Reimb,Lag2Reimb,PrevND,PrevWD,PrevGap,PrescriptionNumerator,NormalizedWeight,
 LaggedNeighborExposure,EligiblePeerCount,HighNeighborAdoptionExposure,LabelNextQuarterEntry,IsCensored,HasEntered,IsObservedZero,IsSuppressed,
 NumericDistribution,WeightedDistribution,AccessGap
FROM X ORDER BY AndaLaunchId,StateId,ObservationQuarter;
""";
        await using var command = new SqlCommand(sql, c) { CommandTimeout = 0 }; await using var r = await command.ExecuteReaderAsync(); var result = new List<AnalysisRow>(180_000);
        while (await r.ReadAsync()) result.Add(new(
            r.GetInt64(0),r.GetInt32(1),r.GetInt32(2),r.GetString(3).Trim(),r.GetString(4),r.GetInt32(5),r.GetString(6),r.GetInt32(7),r.GetInt32(8),
            N<long>(r,9),N<decimal>(r,10),N<long>(r,11),N<long>(r,12),N<decimal>(r,13),N<decimal>(r,14),N<decimal>(r,15),N<decimal>(r,16),N<decimal>(r,17),r.GetInt64(18),r.GetDecimal(19),
            N<decimal>(r,20),r.GetInt32(21),N<bool>(r,22),N<bool>(r,23),r.GetBoolean(24),r.GetBoolean(25),r.GetBoolean(26),r.GetBoolean(27),r.GetDecimal(28),r.GetDecimal(29),r.GetDecimal(30)));
        return result.ToArray();
    }
    private static T? N<T>(SqlDataReader r,int i) where T:struct => r.IsDBNull(i)?null:r.GetFieldValue<T>(i);
    private static float F<T>(T? x) where T:struct,IConvertible => x.HasValue?Convert.ToSingle(x.Value,CultureInfo.InvariantCulture):float.NaN;
    private static int Next(int q)=>q%10==4?(q/10+1)*10+1:q+1;

    private static NextQuarterTrainingRow ToTraining(AnalysisRow x) => new(x.Id,x.LaunchId,x.LaunchId,x.StateId,x.Quarter,x.ApprovalYear,true,false,false,true,x.HasEntered,x.Outcome,
        x.SinceApproval,x.SinceApproval+1,F(x.Rx),F(x.Reimb),F(x.Lag1Rx),F(x.Lag2Rx),F(x.Lag1Reimb),F(x.Lag2Reimb),Growth(x.Rx,x.Lag1Rx),Growth(x.Reimb,x.Lag1Reimb),
        0,0,F(x.PrevND),F(x.PrevWD),F(x.PrevGap),x.BaselineVolume,0,float.NaN,float.NaN,(float)x.Weight,float.NaN,1,float.NaN,float.NaN,F(x.NeighborShare),float.NaN,float.NaN,float.NaN,
        new[]{x.Rx is null,x.Reimb is null,x.Lag1Rx is null,x.Lag2Rx is null,x.PrevND is null,x.NeighborShare is null}.Count(v=>v),x.IsZero,false,x.Suppressed);
    private static float Growth<T>(T? current,T? previous) where T:struct,IConvertible {if(!current.HasValue||!previous.HasValue)return float.NaN;var p=Convert.ToDouble(previous.Value);return p==0?float.NaN:(float)((Convert.ToDouble(current.Value)-p)/p);}
    private static CausalInputRow ToCausal(AnalysisRow x)
    {
        var baseline=x.Quarter%10==1?(x.Quarter/10-1)*10+4:x.Quarter-1;
        var conf=new Dictionary<string,double?>{{"QuarterSinceApproval",x.SinceApproval},{"StateHistoricalMarketWeight",(double)x.Weight},{"PreviousQuarterNumericDistribution",x.PrevND.HasValue?(double)x.PrevND:null},{"PreviousQuarterWeightedDistribution",x.PrevWD.HasValue?(double)x.PrevWD:null},{"PreviousQuarterAccessGap",x.PrevGap.HasValue?(double)x.PrevGap:null},{"Lag1PrescriptionLog",x.Lag1Rx.HasValue?Math.Log(1+x.Lag1Rx.Value):null}};
        return new(x.Id,x.LaunchId,x.LaunchId,x.StateId,x.Region,x.ApprovalYear,x.Quarter,Next(x.Quarter),baseline,x.Quarter,x.SinceApproval,true,false,x.HasEntered,x.Outcome,x.Censored,x.NeighborShare.HasValue?(double)x.NeighborShare:0,x.Treatment,x.Peers,false,"Frozen2021Q1",x.PrevND>=50?"High":"Low",conf,[]);
    }

    private static void WritePredictive(string root,TrainNextQuarterStateEntryResult x)
    {
        File.WriteAllText(Path.Combine(root,"final-predictive-metrics.json"),JsonSerializer.Serialize(new{x.Task,x.ExperimentId,x.Selected,x.Baselines,x.SelectedThreshold,trainingRows=x.Dataset.Training.Count,validationRows=x.Dataset.Validation.Count,testRows=x.Dataset.Test.Count,trainLaunches=x.Dataset.Training.Select(r=>r.GenericLaunchId).Distinct().Count(),validationLaunches=x.Dataset.Validation.Select(r=>r.GenericLaunchId).Distinct().Count(),testLaunches=x.Dataset.Test.Select(r=>r.GenericLaunchId).Distinct().Count(),x.Dataset.CensoredRows,x.Dataset.DatasetHash,x.Dataset.SplitManifestHash},JsonOptions));
        var b=new StringBuilder("feature_row_id,generic_launch_id,state_id,quarter,label,probability,predicted,threshold\n");for(var i=0;i<x.Dataset.Test.Count;i++){var r=x.Dataset.Test[i];var p=x.TestPredictions![i];b.AppendLine($"{r.FeatureRowId},{r.GenericLaunchId},{r.StateId},{r.ObservationQuarterId},{(p.Label?1:0)},{p.Probability.ToString("R",CultureInfo.InvariantCulture)},{(p.PredictedLabel?1:0)},{x.SelectedThreshold?.ToString("R",CultureInfo.InvariantCulture)}");}File.WriteAllText(Path.Combine(root,"final-predictive-test-predictions.csv"),b.ToString());
        var c=new StringBuilder("bin_lower,bin_upper,count,mean_probability,observed_rate\n");foreach(var g in x.TestPredictions!.Select(p=>new{p,bin=Math.Min(9,(int)(p.Probability*10))}).GroupBy(z=>z.bin).OrderBy(g=>g.Key))c.AppendLine($"{g.Key/10d:F1},{(g.Key+1)/10d:F1},{g.Count()},{g.Average(z=>z.p.Probability).ToString("R",CultureInfo.InvariantCulture)},{g.Average(z=>z.p.Label?1d:0d).ToString("R",CultureInfo.InvariantCulture)}");File.WriteAllText(Path.Combine(root,"final-calibration-data.csv"),c.ToString());
        File.WriteAllText(Path.Combine(root,"final-predictive-results.md"),$"# Final predictive results\n\nSelected ML.NET model: {x.Selected?.Trainer}. Model and threshold ({x.SelectedThreshold:G4}) were selected using training/validation only. The locked 2024 test was evaluated once.\n\n```json\n{JsonSerializer.Serialize(x.Selected?.TestMetrics,JsonOptions)}\n```\n\nThese observational predictions are not clinical advice and do not establish causation.\n");
    }
    private static void WriteCausal(string root,RunCausalStudyResult x)
    {
        File.WriteAllText(Path.Combine(root,"final-causal-estimates.json"),JsonSerializer.Serialize(x,JsonOptions));
        Csv(Path.Combine(root,"final-balance-diagnostics.csv"),"variable,treated_mean,control_mean,smd,weighted_treated_mean,weighted_control_mean,weighted_smd,status",x.Balance.Select(z=>$"{z.Variable},{z.TreatedMean:R},{z.ControlMean:R},{z.StandardizedMeanDifference:R},{z.WeightedTreatedMean:R},{z.WeightedControlMean:R},{z.WeightedStandardizedMeanDifference:R},{z.Status}"));
        var p=x.Positivity;Csv(Path.Combine(root,"final-overlap-diagnostics.csv"),"minimum,maximum,treated_minimum,treated_maximum,control_minimum,control_maximum,common_lower,common_upper,outside_support,effective_sample_size,extreme_weights,status",p is null?[]:[$"{p.MinimumPropensity:R},{p.MaximumPropensity:R},{p.TreatedMinimum:R},{p.TreatedMaximum:R},{p.ControlMinimum:R},{p.ControlMaximum:R},{p.CommonSupportLower:R},{p.CommonSupportUpper:R},{p.OutsideCommonSupport},{p.EffectiveSampleSize:R},{p.ExtremeWeightCount},{p.Status}"]);
        Csv(Path.Combine(root,"final-sensitivity-results.csv"),"analysis,status,estimate,description",x.Sensitivity.Select(z=>$"{z.Analysis},{z.Status},{z.Estimate?.ToString("R",CultureInfo.InvariantCulture)},\"{z.Description.Replace("\"","\"\"")}\"").Concat(x.Estimates.Select(z=>$"Estimator_{z.Estimator},{z.Status},{z.Estimate:R},\"{z.Interpretation.Replace("\"","\"\"")}\"")));
        var primary=x.Estimates.FirstOrDefault(z=>z.Estimator==CausalEstimatorKind.AugmentedInverseProbabilityWeighting);File.WriteAllText(Path.Combine(root,"final-causal-results.md"),$"# Final observational causal analysis\n\nUnder the stated identification assumptions, the estimated association interpreted through the approved causal model was an ATT risk difference of {primary?.Estimate:G6} (95% clustered-ANDA bootstrap CI {primary?.ConfidenceLower:G6} to {primary?.ConfidenceUpper:G6}).\n\nThis is observational and depends on consistency, exchangeability, positivity, measurement, and interference assumptions; it is not proof of causation.\n\nBlocking diagnostics: {string.Join("; ",x.BlockingFindings.DefaultIfEmpty("none"))}.\n");
    }
    private static void WriteShared(string root,AnalysisRow[] rows,TrainNextQuarterStateEntryResult p,RunCausalStudyResult c)
    {
        Csv(Path.Combine(root,"final-state-quarter-profile.csv"),"partition,rows,launches,positive_next_entry,censored,suppressed,neighbor_eligible",rows.GroupBy(x=>x.Partition).Select(g=>$"{g.Key},{g.Count()},{g.Select(x=>x.LaunchId).Distinct().Count()},{g.Count(x=>x.Outcome==true)},{g.Count(x=>x.Censored)},{g.Count(x=>x.Suppressed)},{g.Count(x=>x.Peers>=2)}"));
        Csv(Path.Combine(root,"final-temporal-split-profile.csv"),"partition,launches,model_rows,positive_outcomes",new[]{("Training",p.Dataset.Training),("Validation",p.Dataset.Validation),("LockedTest",p.Dataset.Test)}.Select(g=>$"{g.Item1},{g.Item2.Select(x=>x.GenericLaunchId).Distinct().Count()},{g.Item2.Count},{g.Item2.Count(x=>x.LabelNextQuarterEntry==true)}"));
        File.WriteAllText(Path.Combine(root,"final-methodology.md"),"# Final methodology\n\nPrimary unit: ANDA-level official first-generic event. Exact FDA package/product NDC to application to unique eligible ANDA mappings only. Panel: 51 eligible jurisdictions, approval quarter through 2025 Q4. Entry is any positive nonsuppressed prescription count. ND is equal-weight active-state coverage; WD uses frozen 2021 Q1 Medicaid prescription weights. Treatment is prior-quarter eligible-neighbor adoption share >=0.50 with at least two peers. Prediction uses grouped chronological launches; causal estimation uses pretreatment covariates and ATT risk difference.\n");
        File.WriteAllText(Path.Combine(root,"final-limitations.md"),"# Limitations\n\nFDA approval is not commercial launch. Medicaid utilization is not complete national access. Linkage-unavailable launches are excluded and may differ systematically. Suppression and incomplete follow-up reduce information. Neighbor exposure may violate no-interference assumptions. Causal estimates rely on untestable observational identification assumptions.\n");
        File.WriteAllText(Path.Combine(root,"final-executive-summary.md"),$"# Final executive summary\n\nThe frozen analytical population contains {rows.Select(x=>x.LaunchId).Distinct().Count()} ANDA launches and {rows.Length} state-quarter rows. The locked-test predictive PR AUC was {p.Selected?.TestMetrics?.PrAuc:G4}. The primary observational AIPW ATT risk difference was {c.Estimates.FirstOrDefault(x=>x.Estimator==CausalEstimatorKind.AugmentedInverseProbabilityWeighting)?.Estimate:G4}. Interpret both within the documented scope and limitations.\n");
        File.WriteAllText(Path.Combine(root,"final-reproducibility-guide.md"),"# Reproducibility guide\n\nRun the guarded database completion workflow, build the solution, then execute `dotnet run --no-build --project .\\src\\PharmaAccess.Worker\\PharmaAccess.Worker.csproj -- run-real-final-analysis .\\artifacts\\final`. Verify database ownership, source hashes, artifact hashes, and the immutable freeze record before interpreting results.\n");
    }
    private static void Csv(string path,string header,IEnumerable<string> rows)=>File.WriteAllText(path,header+"\n"+string.Join("\n",rows)+"\n");
    private sealed record AnalysisRow(long Id,int LaunchId,int StateId,string StateCode,string Region,int ApprovalYear,string Partition,int Quarter,int SinceApproval,long? Rx,decimal? Reimb,long? Lag1Rx,long? Lag2Rx,decimal? Lag1Reimb,decimal? Lag2Reimb,decimal? PrevND,decimal? PrevWD,decimal? PrevGap,long BaselineVolume,decimal Weight,decimal? NeighborShare,int Peers,bool? Treatment,bool? Outcome,bool Censored,bool HasEntered,bool IsZero,bool Suppressed,decimal ND,decimal WD,decimal Gap);
}
