using Microsoft.EntityFrameworkCore;
using PharmaAccess.Application.Research;
using PharmaAccess.Data.Research;
using PharmaAccess.Domain.Research;
using Xunit;

namespace PharmaAccess.Data.Tests;

public sealed class ResearchImportInitializationTests
{
    private static readonly DateTime At=new(2026,7,21,0,0,0,DateTimeKind.Utc);

    [Fact] public void Twenty_four_manifest_style_registrations_validate_with_authoritative_metadata()
    {
        var sources=Enumerable.Range(0,24).Select(i=>Source(i,(ImportSourceKind)(i%6))).ToArray();
        var plan=ResearchImportInitializationFactory.Build(Request(sources),At);
        Assert.Equal(24,plan.Sources.Count);Assert.Equal(24,plan.Dataset.TotalSourceFiles);Assert.Equal("real-2021-2025-v1",plan.Dataset.VersionCode.Value);
        Assert.All(plan.Sources,x=>{Assert.False(Path.IsPathRooted(x.Registration.RegisteredPath));Assert.DoesNotContain('\\',x.Registration.RegisteredPath);Assert.True(x.Registration.OfficialSourceReference.StartsWith("https://",StringComparison.OrdinalIgnoreCase)||x.Registration.OfficialSourceReference.StartsWith("repository://",StringComparison.OrdinalIgnoreCase));Assert.NotEqual("Official public source",x.Registration.SourceAuthority);Assert.NotEqual("profile-defined",x.Registration.Delimiter);});
        Assert.Equal(6,plan.Sources.Select(x=>x.Registration.SourceType).Distinct().Count());
    }

    [Fact] public void Original_failure_identifies_overlength_delimiter()
    {
        var error=Assert.Throws<ArgumentException>(()=>new ResearchSourceRegistration("src",ResearchSourceType.FdaFirstGenericApproval,"FDA","First Generic Drug Approvals","description","https://www.fda.gov/",At,null,null,"public data","schema","fda.csv","fda/converted/fda.csv",new string('A',64),1,"utf-8","profile-defined",false,"worker",At));
        Assert.Equal("delimiter",error.ParamName);Assert.Contains("maximum length of 8",error.Message);
    }

    [Fact] public void Missing_field_has_field_specific_diagnostic()
    {
        var bad=Source(0,ImportSourceKind.Fda) with{Encoding=" "};var error=Assert.Throws<ArgumentException>(()=>ResearchImportInitializationFactory.Build(Request([bad]),At));Assert.Equal("encoding",error.ParamName);Assert.Contains("cannot be blank",error.Message);
    }

    [Fact] public void Overlength_field_fails_before_any_entity_is_tracked_and_version_remains_reusable()
    {
        var options=new DbContextOptionsBuilder<Data.PharmaAccessDbContext>().UseSqlServer("Server=localhost;Database=MetadataOnly;Trusted_Connection=True;TrustServerCertificate=True").Options;using var db=new Data.PharmaAccessDbContext(options);var bad=Source(0,ImportSourceKind.Fda)with{Delimiter=new string('x',9)};
        Assert.Throws<ArgumentException>(()=>ResearchImportInitializationFactory.Build(Request([bad]),At));Assert.Empty(db.ChangeTracker.Entries());
        var valid=ResearchImportInitializationFactory.Build(Request([Source(0,ImportSourceKind.Fda)]),At);Assert.Equal("real-2021-2025-v1",valid.Dataset.VersionCode.Value);Assert.Empty(db.ChangeTracker.Entries());
    }

    private static ImportInitialization Request(IReadOnlyList<ImportSourceRegistration> sources)=>new("real-2021-2025-v1",1,"m9b-final-preview-006",new string('B',64),sources,new string('C',40));
    private static ImportSourceRegistration Source(int i,ImportSourceKind kind)=>new($"category-{kind.ToString().ToLowerInvariant()}/source-{i:00}.csv",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"source-{i}"))),100+i,"schema-v1",kind,10,true,"utf-8",kind==ImportSourceKind.StateAdjacency?"|":",",new DateOnly(2021,1,1),new DateOnly(2025,12,31));
}
