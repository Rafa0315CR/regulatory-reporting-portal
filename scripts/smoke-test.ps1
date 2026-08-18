param([string]$BaseUrl = 'http://127.0.0.1:5074')

$ErrorActionPreference = 'Stop'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Assert-Equal($Actual, $Expected, $Message) {
    if ($Actual -ne $Expected) {
        throw "$Message Expected '$Expected', received '$Actual'."
    }
}

$health = Invoke-RestMethod -Uri "$BaseUrl/api/health"
Assert-Equal $health.status 'healthy' 'Health endpoint failed.'

$unauthorizedStatus = try {
    Invoke-WebRequest -Method Post -Uri "$BaseUrl/api/reports" -ContentType 'application/json' -Body '{"standard":"CRS"}' -ErrorAction Stop | Select-Object -ExpandProperty StatusCode
} catch {
    [int]$_.Exception.Response.StatusCode
}
Assert-Equal $unauthorizedStatus 401 'Anonymous write protection failed.'

$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/session/login" -WebSession $session -ContentType 'application/json' -Body '{"username":"analyst","password":"Analyst2026!"}'
Assert-Equal $login.role 'Analyst' 'Analyst login failed.'

$invalidStatus = try {
    Invoke-WebRequest -Method Post -Uri "$BaseUrl/api/clients" -WebSession $session -ContentType 'application/json' -Body '{"legalName":"X","taxIdentificationNumber":"1","countryCode":"CRI","dateOfBirth":"2015-01-01","accountBalance":-1,"currency":"ABC"}' -ErrorAction Stop | Select-Object -ExpandProperty StatusCode
} catch {
    [int]$_.Exception.Response.StatusCode
}
Assert-Equal $invalidStatus 400 'Client validation failed.'

$report = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/reports" -WebSession $session -ContentType 'application/json' -Body '{"standard":"CRS"}'
Assert-Equal $report.standard 'CRS' 'Report creation failed.'

$xml = Invoke-WebRequest -Uri "$BaseUrl/api/reports/$($report.id)/xml" -WebSession $session
Assert-Equal $xml.StatusCode 200 'XML endpoint failed.'
if (!$xml.Content.Contains('<Records count="2">')) { throw 'XML did not contain the expected records.' }

Write-Output 'All smoke tests passed.'
