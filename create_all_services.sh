#!/bin/bash
set -e

BASE="/home/claude/FinancialTradingPlatform"
cd "$BASE"

echo "Creating all microservices and components..."

# Function to create a microservice
create_service() {
    local SERVICE_NAME=$1
    local SERVICE_DIR="src/Services/${SERVICE_NAME}.API"
    
    mkdir -p "${SERVICE_DIR}"/{Controllers,Commands,Queries,Handlers,Models}
    
    # Create csproj
    cat > "${SERVICE_DIR}/${SERVICE_NAME}.API.csproj" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.2.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    <PackageReference Include="Microsoft.Identity.Web" Version="2.16.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Shared\Common\Common.csproj" />
    <ProjectReference Include="..\..\Shared\EventContracts\EventContracts.csproj" />
    <ProjectReference Include="..\..\Shared\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>
</Project>
EOF

    echo "✅ Created ${SERVICE_NAME}.API"
}

# Create all services
create_service "TradingEngine"
create_service "Portfolio"
create_service "RiskAnalysis"
create_service "MarketData"
create_service "Reporting"
create_service "Notification"

echo "✅ All services created!"
