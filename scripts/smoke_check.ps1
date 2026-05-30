$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\\PharmacovigilanceSignalRouter.Api\\PharmacovigilanceSignalRouter.Api.csproj"
$port = 5094
$base = "http://127.0.0.1:$port"
$routes = @(
  "/",
  "/signal-lane",
  "/case-routing",
  "/safety-posture",
  "/verification",
  "/docs",
  "/api/dashboard/summary",
  "/api/signal-lane",
  "/api/case-routing",
  "/api/safety-posture",
  "/api/verification",
  "/api/sample"
)

$job = Start-Job -ScriptBlock {
  param($projectPath, $listenPort)
  dotnet run --project $projectPath --urls "http://127.0.0.1:$listenPort"
} -ArgumentList $project, $port

try {
  $ready = $false
  for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Milliseconds 500
    try {
      $probe = Invoke-WebRequest -Uri "$base/" -UseBasicParsing -TimeoutSec 3
      if ($probe.StatusCode -eq 200) {
        $ready = $true
        break
      }
    } catch {
    }
  }

  if (-not $ready) {
    throw "Local server never became ready."
  }

  foreach ($route in $routes) {
    $response = Invoke-WebRequest -Uri "$base$route" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -ne 200) {
      throw "Smoke check failed for $route with status $($response.StatusCode)"
    }
  }

  Write-Host "Smoke check passed."
}
finally {
  Stop-Job $job -ErrorAction SilentlyContinue | Out-Null
  Remove-Job $job -Force -ErrorAction SilentlyContinue | Out-Null
}
