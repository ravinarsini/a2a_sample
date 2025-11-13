# Agent4 Test Script
# PowerShell script to test Agent4 functionality

Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?        Agent4 News Search Test Script   ?" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Configuration
$baseUrl = "http://localhost:5050"
$discoveryUrl = "http://localhost:5000"

Write-Host "?? Step 1: Checking if services are running..." -ForegroundColor Yellow
Write-Host ""

# Check Discovery Service
try {
    $discoveryResponse = Invoke-RestMethod -Uri "$discoveryUrl/list" -Method Get -TimeoutSec 5
    Write-Host "? Discovery Service is running" -ForegroundColor Green
    
    # Check if Agent4 is registered
    $agents = $discoveryResponse
 $agent4Found = $false
    foreach ($key in $agents.Keys) {
        if ($agents[$key].skill -eq "news") {
            $agent4Found = $true
        Write-Host "? Agent4 (news) is registered" -ForegroundColor Green
      Write-Host "   Address: $($agents[$key].Address)" -ForegroundColor Gray
         break
        }
    }
    
    if (-not $agent4Found) {
        Write-Host "? Agent4 not found in discovery service" -ForegroundColor Red
        Write-Host "   Please start Agent4 first!" -ForegroundColor Red
   exit 1
    }
}
catch {
    Write-Host "? Discovery Service is not running" -ForegroundColor Red
    Write-Host "   Please start it with: dotnet run --project Agent2AgentProtocol.Discovery.Service" -ForegroundColor Red
    exit 1
}

# Check Agent1 API
try {
    $agent1Response = Invoke-RestMethod -Uri "$baseUrl/swagger/v1/swagger.json" -Method Get -TimeoutSec 5
    Write-Host "? Agent1 API is running" -ForegroundColor Green
}
catch {
    Write-Host "? Agent1 API is not running" -ForegroundColor Red
    Write-Host "   Please start it with: dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent1" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "?? Step 2: Running test searches..." -ForegroundColor Yellow
Write-Host ""

# Test cases
$testCases = @(
    @{ Topic = "artificial intelligence"; Description = "AI and machine learning" },
    @{ Topic = "climate change"; Description = "Environmental news" },
    @{ Topic = "technology"; Description = "Tech industry updates" }
)

$successCount = 0
$failCount = 0

foreach ($test in $testCases) {
    Write-Host "?? Testing: $($test.Description)" -ForegroundColor Cyan
    Write-Host "   Query: news: $($test.Topic)" -ForegroundColor Gray
    
    try {
        $body = "news: $($test.Topic)"
    $response = Invoke-RestMethod -Uri "$baseUrl/api/client/post" `
            -Method Post `
         -ContentType "application/json" `
     -Body (ConvertTo-Json $body) `
            -TimeoutSec 90
        
    if ($response.responses -and $response.responses.Count -gt 0) {
            $agentResponse = $response.responses[0]
            
       if ($agentResponse.success) {
        Write-Host "   ? Success!" -ForegroundColor Green
           
          # Extract file path from response
if ($agentResponse.response -match "File created: (.+\.txt)") {
        $filePath = $matches[1]
            Write-Host "   ?? File: $filePath" -ForegroundColor Gray
       
             # Check if file exists
    if (Test-Path $filePath) {
    $fileSize = (Get-Item $filePath).Length
   Write-Host "   ?? Size: $fileSize bytes" -ForegroundColor Gray
               }
        }
      
      $successCount++
          }
 else {
  Write-Host "   ? Failed: $($agentResponse.error)" -ForegroundColor Red
     $failCount++
    }
        }
      else {
     Write-Host "   ? No response received" -ForegroundColor Red
       $failCount++
        }
    }
    catch {
        Write-Host "   ? Error: $($_.Exception.Message)" -ForegroundColor Red
     $failCount++
    }
    
    Write-Host ""
 Start-Sleep -Seconds 2  # Brief pause between requests
}

# Summary
Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?    Test Summary               ?" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total Tests: $($testCases.Count)" -ForegroundColor White
Write-Host "? Passed: $successCount" -ForegroundColor Green
Write-Host "? Failed: $failCount" -ForegroundColor Red
Write-Host ""

if ($successCount -gt 0) {
    Write-Host "?? Check NewsResults folder for created files" -ForegroundColor Yellow
    $newsResultsPath = Join-Path $PSScriptRoot "Semantic.Kernel.Agent2AgentProtocol.Example.Agent4\NewsResults"
    if (Test-Path $newsResultsPath) {
      $files = Get-ChildItem $newsResultsPath -Filter "*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 5
if ($files) {
         Write-Host ""
      Write-Host "Recent files:" -ForegroundColor Gray
       foreach ($file in $files) {
    Write-Host "  • $($file.Name) ($([math]::Round($file.Length/1KB, 2)) KB)" -ForegroundColor Gray
            }
    }
    }
}

Write-Host ""
if ($failCount -eq 0 -and $successCount -gt 0) {
    Write-Host "?? All tests passed! Agent4 is working correctly." -ForegroundColor Green
}
elseif ($failCount -gt 0) {
    Write-Host "??  Some tests failed. Please check:" -ForegroundColor Yellow
    Write-Host "   1. Is your OpenAI API key configured?" -ForegroundColor Yellow
    Write-Host "   2. Do you have internet connectivity?" -ForegroundColor Yellow
    Write-Host "   3. Check Agent4 console for error messages" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
