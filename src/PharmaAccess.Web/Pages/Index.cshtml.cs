using Microsoft.AspNetCore.Mvc.RazorPages;
using PharmaAccess.Application.Research;
namespace PharmaAccess.Web.Pages;
public sealed class IndexModel(IFinalizedResearchReadService research) : PageModel { public FinalizedDatasetSnapshot Data {get;private set;}=null!;public async Task OnGet(CancellationToken ct)=>Data=await research.GetAsync(ct); }
