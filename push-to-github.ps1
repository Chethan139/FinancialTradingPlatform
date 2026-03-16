$token = "YOUR_GITHUB_TOKEN_HERE"
$username = "Chethan139"
$repoName = "FinancialTradingPlatform"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Pushing to GitHub..." -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Configure Git
Write-Host "Configuring Git..." -ForegroundColor Yellow
git config user.email "chethan139@github.com"
git config user.name "Chethan139"

# Initialize if needed
if (!(Test-Path .git)) {
    Write-Host "Initializing Git repository..." -ForegroundColor Yellow
    git init
    git branch -M main
} else {
    Write-Host "Git repository already initialized" -ForegroundColor Green
}

# Add and commit
Write-Host "Staging files..." -ForegroundColor Yellow
git add .

Write-Host "Creating commit..." -ForegroundColor Yellow
git commit -m "Initial commit: Azure Financial Trading Platform

- 6 Microservices (Trading Engine, Portfolio, Risk Analysis, Market Data, Reporting, Notifications)
- CQRS + Event Sourcing patterns
- Azure Cosmos DB + SQL Server (Polyglot Persistence)
- Service Bus + Event Hubs integration
- Resilience patterns (Circuit Breaker, Retry, Rate Limiting)
- JWT authentication with Azure AD
- Docker Compose for local development
- Comprehensive documentation and interview Q&A"

# Create GitHub repository
Write-Host "Creating GitHub repository..." -ForegroundColor Yellow

$headers = @{
    "Authorization" = "token $token"
    "Accept" = "application/vnd.github.v3+json"
}

$body = @{
    "name" = $repoName
    "description" = "Production-grade Azure Financial Trading Platform showcasing microservices, CQRS, Event Sourcing, Cosmos DB, Service Bus, and enterprise .NET patterns"
    "private" = $false
    "auto_init" = $false
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "https://api.github.com/user/repos" -Method Post -Headers $headers -Body $body -ContentType "application/json"
    Write-Host "? GitHub repository created successfully" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 422) {
        Write-Host "? Repository already exists - will push to existing repo" -ForegroundColor Yellow
    } else {
        Write-Host "? Error creating repository: $_" -ForegroundColor Yellow
        Write-Host "  Continuing with push anyway..." -ForegroundColor Yellow
    }
}

# Push to GitHub
Write-Host "Pushing code to GitHub..." -ForegroundColor Yellow

git remote remove origin 2>$null
git remote add origin "https://${token}@github.com/${username}/${repoName}.git"

try {
    git push -u origin main --force
    Write-Host "? Code pushed successfully!" -ForegroundColor Green
} catch {
    Write-Host "? Push failed: $_" -ForegroundColor Red
    exit 1
}

# Clean up token from config
Write-Host "Cleaning up credentials..." -ForegroundColor Yellow
git remote set-url origin "https://github.com/${username}/${repoName}.git"

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "? SUCCESS!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Your repository is now live at:" -ForegroundColor White
Write-Host "https://github.com/${username}/${repoName}" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Visit the repository and add topics (azure, microservices, dotnet, etc.)" -ForegroundColor White
Write-Host "2. Update README.md with your profile information" -ForegroundColor White
Write-Host "3. Star your repository!" -ForegroundColor White
Write-Host ""
