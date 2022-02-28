using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using PowerBiEmbed.Models;
using PowerBiEmbed.ViewModels;

namespace PowerBiEmbed.Services
{
public class PowerBiApiService
{
	private IConfiguration _configuration;
	private ITokenAcquisition _tokenAcquisition;
	private Uri _powerBiServiceApiRootUrl;
	private Guid _workspaceId;
		
	public const string PowerBiDefaultScope = "https://analysis.windows.net/powerbi/api/.default";

	public PowerBiApiService(IConfiguration configuration, ITokenAcquisition tokenAcquisition)
	{
		_configuration = configuration;
		_powerBiServiceApiRootUrl = new Uri(configuration["PowerBi:ServiceRootUrl"]);
		_workspaceId = new Guid(configuration["PowerBi:WorkspaceId"]);
		_tokenAcquisition = tokenAcquisition;
	}
    public string GetAccessToken()
{
	return _tokenAcquisition.GetAccessTokenForAppAsync(PowerBiDefaultScope).Result;
}
public PowerBIClient GetPowerBiClient()
{
	var tokenCredentials = new TokenCredentials(GetAccessToken(), "Bearer");
	
	return new PowerBIClient(_powerBiServiceApiRootUrl, tokenCredentials);
}

public async Task<WorkspaceViewModel> GetReportsEmbeddingData()
{
	// Connect to Power BI
	var client = GetPowerBiClient();
	
	// Get reports in the workspace
	var reports = (await client.Reports.GetReportsInGroupAsync(_workspaceId)).Value;
	
	var reportList = new List<EmbeddedReport>();
	var reportTokenRequests = new List<GenerateTokenRequestV2Report>();
	
	foreach (var report in reports)
	{
		reportList.Add(new EmbeddedReport
		{
			Id = report.Id.ToString(),
			Name = report.Name,
			EmbedUrl = report.EmbedUrl
		});
		
		reportTokenRequests.Add(new GenerateTokenRequestV2Report(report.Id, allowEdit: true));
	}
	
	// Get datasets in the workspace
	var datasets = (await client.Datasets.GetDatasetsInGroupAsync(_workspaceId)).Value;
	
	var datasetList = new List<EmbeddedDataset>();
	var datasetTokenRequests = new List<GenerateTokenRequestV2Dataset>();
	
	foreach (var dataset in datasets)
	{
		datasetList.Add(new EmbeddedDataset
		{
			Id = dataset.Id.ToString(),
			Name = dataset.Name,
			EmbedUrl = dataset.QnaEmbedURL
		});
		
		datasetTokenRequests.Add(new GenerateTokenRequestV2Dataset(dataset.Id));
	}
	
// Create effective identity for the first dataset
var datasetId = datasets[0].Id.ToString();            
var effectiveIdentities = new List<EffectiveIdentity>() {
	new EffectiveIdentity(
		username: _configuration["CurrentUser:UserName"],
		roles: new List<string> {"AppUser"},
		datasets: new List<string> {datasetId})
};



	// Generate token request for the workspace
	var workspaceRequests = new GenerateTokenRequestV2TargetWorkspace[] {
		new GenerateTokenRequestV2TargetWorkspace(_workspaceId)
	};
	
	// Bundle token requests for reports, datasets, and the workspace
	var tokenRequest = new GenerateTokenRequestV2(
		reports: reportTokenRequests,
		datasets: datasetTokenRequests,
		targetWorkspaces: workspaceRequests,
		identities: effectiveIdentities
	);
	
	// Generate the embed token
	string embedToken = (await client.EmbedToken.GenerateTokenAsync(tokenRequest)).Token;
	
	// Return report embedding data to caller
	return new WorkspaceViewModel
	{
		ReportsJson = JsonConvert.SerializeObject(reportList),
		DatasetsJson = JsonConvert.SerializeObject(datasetList),
		EmbedToken = embedToken
	};
}


}
}