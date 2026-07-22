using Microsoft.AspNetCore.Mvc.RazorPages;using PharmaAccess.Application.Research;
namespace PharmaAccess.Web.Pages;
public sealed class DashboardModel(IFinalResearchArtifactService artifacts) : PageModel { public FinalResearchResults Results {get;private set;}=null!;public void OnGet()=>Results=artifacts.Read(); }
