[CmdletBinding()]
param([string]$ApiImage='pharmaaccess-api:local',[string]$WebImage='pharmaaccess-web:local')
$ErrorActionPreference='Stop'
$apiName='pharmaaccess-api-smoke';$webName='pharmaaccess-web-smoke'
try {
    & docker run -d --rm --name $apiName -p 127.0.0.1::8080 -e ASPNETCORE_ENVIRONMENT=Production -e Authentication__Mode=Jwt $ApiImage | Out-Null
    & docker run -d --rm --name $webName -p 127.0.0.1::8080 -e ASPNETCORE_ENVIRONMENT=Production -e Authentication__Mode=Jwt -e PharmaAccess__ApiBaseUrl=http://host.docker.internal:1/ $WebImage | Out-Null
    foreach($name in @($apiName,$webName)){foreach($attempt in 1..30){$health=& docker inspect $name --format '{{.State.Health.Status}}';if($health -eq 'healthy'){break};Start-Sleep -Seconds 1};if($health -ne 'healthy'){throw "$name did not become healthy."}}
    $apiPort=& docker port $apiName 8080/tcp;$apiPort=($apiPort -split ':')[-1]
    $webPort=& docker port $webName 8080/tcp;$webPort=($webPort -split ':')[-1]
    $live=Invoke-WebRequest "http://127.0.0.1:$apiPort/health/live" -UseBasicParsing
    $ready=Invoke-WebRequest "http://127.0.0.1:$apiPort/health/ready" -UseBasicParsing
    $version=Invoke-WebRequest "http://127.0.0.1:$apiPort/version" -UseBasicParsing
    $assistant=Invoke-WebRequest "http://127.0.0.1:$apiPort/api/v1/assistant/ask" -Method Post -ContentType 'application/json' -Body '{"question":"Is Gemini required?"}' -UseBasicParsing
    try { Invoke-WebRequest "http://127.0.0.1:$apiPort/api/v1/model-governance/drift-reports" -Method Post -ContentType 'application/json' -Body '{}' -UseBasicParsing | Out-Null; throw 'Governance request was not rejected.' } catch { if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw } }
    $webLive=Invoke-WebRequest "http://127.0.0.1:$webPort/health/live" -UseBasicParsing
    try { Invoke-WebRequest "http://127.0.0.1:$webPort/health/ready" -UseBasicParsing | Out-Null; throw 'Web readiness did not reflect unavailable API.' } catch { if ($_.Exception.Response.StatusCode.value__ -ne 503) { throw } }
    if(@($live,$ready,$version,$assistant,$webLive)|Where-Object StatusCode -ne 200){throw 'A smoke endpoint failed.'}
    if($assistant.Content -notmatch 'provider-unavailable'){throw 'Assistant did not return the safe Gemini fallback.'}
}
finally { & docker rm -f $apiName $webName 2>$null | Out-Null }
