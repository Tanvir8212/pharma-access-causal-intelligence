namespace PharmaAccess.Application.Research;

public sealed record FinalizedDatasetSnapshot(string DatasetVersion,string Status,string ValidationStatus,long TotalRows,DateTime? FinalizedAtUtc,int EligibleLaunches,int TrainLaunches,int ValidationLaunches,int TestLaunches);
public sealed record PredictiveResultSnapshot(double RocAuc,double PrAuc,double LogLoss,double BrierScore,double Precision,double Recall,double F1,double Specificity);
public sealed record CausalResultSnapshot(double Aipw,double ConfidenceLower,double ConfidenceUpper,double Iptw,double OutcomeRegression,double Unadjusted,double PythonAipw);
public sealed record FinalResearchResults(PredictiveResultSnapshot Predictive,CausalResultSnapshot Causal,bool ArtifactsAvailable);
public sealed record ResearchAnswer(string Answer,IReadOnlyList<string> Sources);

public interface IFinalizedResearchReadService { Task<FinalizedDatasetSnapshot> GetAsync(CancellationToken cancellationToken=default); }
public interface IFinalResearchArtifactService { FinalResearchResults Read(); }
public interface IResearchAnswerService { Task<ResearchAnswer> AnswerAsync(string question,CancellationToken cancellationToken=default); }

public sealed class DeterministicResearchAnswerService(IFinalizedResearchReadService data,IFinalResearchArtifactService artifacts) : IResearchAnswerService
{
    public async Task<ResearchAnswer> AnswerAsync(string question,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(question))throw new ArgumentException("Question is required.",nameof(question));
        if(question.Trim().Length>500)throw new ArgumentException("Question cannot exceed 500 characters.",nameof(question));
        var q=question.Trim().ToLowerInvariant();var d=await data.GetAsync(ct);var r=artifacts.Read();
        ResearchAnswer A(string answer,params string[] sources)=>new(answer,sources);
        if(q.Contains("summarize")||q.Contains("project"))return A($"PharmaAccess AI analyzed {d.EligibleLaunches} eligible ANDA-level generic launches across {d.TotalRows:N0} state-quarter rows. The finalized study reports locked-test ROC AUC {r.Predictive.RocAuc:F4} and a governed observational AIPW ATT risk difference of {r.Causal.Aipw:F5}.","core.DatasetVersion","artifacts/final/final-predictive-metrics.json","artifacts/final/final-causal-estimates.json");
        if(q.Contains("how many launches")||q.Contains("launches were analyzed"))return A($"{d.EligibleLaunches} eligible ANDA-level launches were analyzed.","research.AndaLaunch","artifacts/final/final-cohort-flow.csv");
        if(q.Contains("state-quarter")||q.Contains("rows were used"))return A($"The finalized analytical panel contains {d.TotalRows:N0} state-quarter rows.","core.DatasetVersion","research.AndaStateQuarterPanel");
        if(q.Contains("roc auc"))return A($"The locked-test ROC AUC was {r.Predictive.RocAuc:F4}.","artifacts/final/final-predictive-metrics.json");
        if(q.Contains("how good")||q.Contains("predictive model"))return A($"On the locked test partition, ROC AUC was {r.Predictive.RocAuc:F4}, PR AUC {r.Predictive.PrAuc:F4}, log loss {r.Predictive.LogLoss:F4}, and Brier score {r.Predictive.BrierScore:F4}. Performance is useful but positive-class precision and recall remain limited, so this is not a clinical decision tool.","artifacts/final/final-predictive-metrics.json");
        if(q.Contains("cause")||q.Contains("causal result"))return A($"The governed .NET AIPW ATT risk difference was {r.Causal.Aipw:F5}, with 95% CI {r.Causal.ConfidenceLower:F5} to {r.Causal.ConfidenceUpper:F5}. The interval includes zero, so the analysis did not provide clear evidence of a nonzero causal effect under the stated observational assumptions. It does not establish that neighboring-state adoption causes adoption.","artifacts/final/final-causal-estimates.json","artifacts/final/final-causal-results.md");
        if(q.Contains("python")||q.Contains("difference between"))return A($"The governed .NET AIPW estimate was {r.Causal.Aipw:F5}; the grouped cross-fitted Python robustness estimate was {r.Causal.PythonAipw:F5}. The material difference reflects different nuisance-model fitting procedures and is disclosed as a methodological limitation, not forced into agreement.","artifacts/final/final-causal-estimates.json","artifacts/final/final-python-validation-report.json");
        if((q.Contains("nd")&&q.Contains("wd"))||q.Contains("access gap"))return A("Numeric Distribution (ND) is the percentage of eligible states with observed entry. Weighted Distribution (WD) weights entered states by the frozen historical market weights. Access Gap is WD minus ND, describing whether entry is concentrated in higher-weight markets.","docs/research/REAL_FEATURE_BUILD.md","artifacts/final/final-methodology.md");
        if(q.Contains("limitation"))return A("Main limitations include observational identification assumptions, residual confounding, partial interference, overlap and measurement limitations, suppressed or missing utilization, and the material difference between non-cross-fitted .NET and grouped cross-fitted Python nuisance-model procedures.","artifacts/final/final-limitations.md");
        if(q.Contains("train")&&q.Contains("validation")&&q.Contains("test"))return A($"The grouped chronological split used {d.TrainLaunches} training launches, {d.ValidationLaunches} validation launches, and {d.TestLaunches} locked-test launches.","artifacts/final/final-temporal-split-profile.csv");
        return A("This first local version supports questions about the finalized cohort, predictive results, causal results, methodology, ND, WD, Access Gap and study limitations.","Supported-question catalog");
    }
}
