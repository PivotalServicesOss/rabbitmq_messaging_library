$script:project_config = "Debug"

properties {
  $base_dir = resolve-path .
  $publish_dir = "$base_dir\publish-artifacts"
  $solution_file = "$base_dir\$solution_name.sln"
  $test_dir = "$base_dir\test"
  $local_nuget_repo = "/Users/ajaganathan/.nugetrepo"
  $remote_nuget_repo = "https://api.nuget.org/v3/index.json"
  $remote_myget_repo = "https://www.myget.org/F/ajaganathan/api/v3/index.json"
  $date = Get-Date 
  $dotnet_exe = get-dotnet
  $nuget = "$dotnet_exe nuget"
}

#These are aliases for other build tasks. They typically are named after the camelcase letters (rd = Rebuild Databases)
task default -depends DevBuild
task cib -depends CiBuild
task cipk -depends CiPack
task dpk -depends DevPack
task dr -depends DevPublish
task cirn -depends CiPublish2Nuget
task cirm -depends CiPublish2Myget
task ? -depends help


task emitProperties {
  Write-Host "solution_name=$solution_name"
  Write-Host "solution_file=$solution_file"
  Write-Host "test_dir=$test_dir"
  Write-Host "publish_dir=$publish_dir"
  Write-Host "project_config=$project_config"
}

task help {
   Write-Help-Header
   Write-Help-Section-Header "Comprehensive Building"
   Write-Help-For-Alias "(default)" "Intended for first build or when you want a fresh, clean local copy"
   Write-Help-For-Alias "ci" "Continuous Integration build (long and thorough) with packaging"
   Write-Help-Footer
   exit 0
}

#These are the actual build tasks. They should be Pascal case by convention
task DevBuild -depends SetDebugBuild, emitProperties, Clean, Restore, Compile, UnitTest, IntegrationTests
task DevPack -depends DevBuild, Pack
task DevPublish -depends DevPack, Push2Local
task CiBuild -depends SetReleaseBuild, emitProperties, Clean, Restore, Compile, UnitTest, IntegrationTests
task CiPack -depends CiBuild, Pack
task CiPublish2Nuget -depends CiPack, Push2Nuget
task CiPublish2Myget -depends CiPack, Push2Myget

task SetDebugBuild {
    $script:project_config = "Debug"
}

task SetReleaseBuild {
    $script:project_config = "Release"
}

task Restore {
    Write-Host "******************* Now restoring the solution dependencies *********************"
    exec { 
        & $dotnet_exe msbuild /t:restore $solution_file /v:m /p:NuGetInteractive="true"
        if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
    }
}

task Clean {
    Write-Host "******************* Now cleaning the solution and artifacts *********************"
    if (Test-Path $publish_dir) {
        delete_directory $publish_dir
    }
    exec { 
        & $dotnet_exe msbuild /t:clean /v:m /p:Configuration=$project_config $solution_file 
    }
    if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
}

task Compile {
    Write-Host "******************* Now compiling the solution *********************"
    exec { 
        & $dotnet_exe msbuild /t:build /v:m /p:Configuration=$project_config /nologo /p:Platform="Any CPU" /nologo $solution_file 
    }
    if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
}

task UnitTest {
    Write-Host "******************* Now running unit tests *********************"
    Push-Location $base_dir
    $test_projects = @((Get-ChildItem -Recurse -Filter "*Unit.Test.csproj").FullName) -join '~'

    foreach($test_project in $test_projects.Split("~"))
    {
        Write-Host "Executing tests on: $test_project"
        exec {
            & $dotnet_exe test $test_project --settings "$base_dir/.runsettings" -- xunit.parallelizeTestCollections=true
        }
    }
    Pop-Location
    if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
 }

 task IntegrationTests {
    Write-Host "******************* Now running integration tests *********************"
    Push-Location $base_dir
    $test_projects = @((Get-ChildItem -Recurse -Filter "*Integration.Test.csproj").FullName) -join '~'

    foreach($test_project in $test_projects.Split("~"))
    {
        Write-Host "Executing tests on: $test_project"
        exec {
            & $dotnet_exe test $test_project -- xunit.parallelizeTestCollections=false
        }
    }
    Pop-Location
    if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
 }

 task Pack {
    Write-Host "******************* Now creating nuget package(s) *********************"
	Push-Location $base_dir
	$projects = @(Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object {$_.Directory -like '*src*'}).FullName	

	foreach ($project in $projects) {
		Write-Host "Executing nuget pack on the project: $project"
		exec { 
            & $dotnet_exe msbuild /t:pack /v:m $project /p:OutputPath=$publish_dir /p:Configuration=$project_config
            if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
        }
	}

	Pop-Location
    if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
}

task Push2Local {
    Write-Host "******************* Now pushing available nuget package(s) to $local_nuget_repo *********************"
	Push-Location $base_dir
	$packages = @(Get-ChildItem -Recurse -Filter "*.nupkg" | Where-Object {$_.Directory -like "*publish-artifacts*"}).FullName
	if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}

	foreach ($package in $packages) {
		Write-Host "Executing nuget add for the package: $package"
		exec { & $nuget add $package -Source $local_nuget_repo -Force}
        if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
        Write-Host "Warning: Possible overwrite of existing package $package, possible solution is to clear the cache(S)" -ForegroungColor Yellow
	}

	Pop-Location
    if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
}

task Push2Nuget {
    Write-Host "******************* Now pushing available nuget package(s) to nuget.org *********************"
	Push-Location $base_dir
	$packages = @(Get-ChildItem -Recurse -Filter "*.nupkg" | Where-Object {$_.Directory -like "*publish-artifacts*"}).FullName
	if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}

	foreach ($package in $packages) {
		Write-Host "Executing nuget push for the package: $package"
		exec { & $nuget push $package -Source $remote_nuget_repo -ApiKey $api_key}
        if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
	}

	Pop-Location
    if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
}

task Push2Myget {
    Write-Host "******************* Now pushing available nuget package(s) to myget.org *********************"
	Push-Location $base_dir
	$packages = @(Get-ChildItem -Recurse -Filter "*.nupkg" | Where-Object {$_.Directory -like "*publish-artifacts*"}).FullName
	if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}

	foreach ($package in $packages) {
		Write-Host "Executing nuget push for the package: $package, apikey: $api_key"
		exec { & $nuget push $package -Source $remote_myget_repo -ApiKey $api_key}
        if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
	}

	Pop-Location
    if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
}


# -------------------------------------------------------------------------------------------------------------
# generalized functions for Help Section
# --------------------------------------------------------------------------------------------------------------

function Write-Help-Header($description) {
   Write-Host ""
   Write-Host "********************************" -foregroundcolor DarkGreen -nonewline;
   Write-Host " HELP " -foregroundcolor Green  -nonewline;
   Write-Host "********************************"  -foregroundcolor DarkGreen
   Write-Host ""
   Write-Host "This build script has the following common build " -nonewline;
   Write-Host "task " -foregroundcolor Green -nonewline;
   Write-Host "aliases set up:"
}

function Write-Help-Footer($description) {
   Write-Host ""
   Write-Host " For a complete list of build tasks, view default.ps1."
   Write-Host ""
   Write-Host "**********************************************************************" -foregroundcolor DarkGreen
}

function Write-Help-Section-Header($description) {
   Write-Host ""
   Write-Host " $description" -foregroundcolor DarkGreen
}

function Write-Help-For-Alias($alias,$description) {
   Write-Host "  > " -nonewline;
   Write-Host "$alias" -foregroundcolor Green -nonewline;
   Write-Host " = " -nonewline;
   Write-Host "$description"
}

# -------------------------------------------------------------------------------------------------------------
# generalized functions
# --------------------------------------------------------------------------------------------------------------

function global:delete_file($file) {
    if($file) { remove-item $file -force -ErrorAction SilentlyContinue | out-null }
}

function global:delete_directory($directory_name)
{
  rd $directory_name -recurse -force  -ErrorAction SilentlyContinue | out-null
}

function global:delete_files($directory_name) {
    Get-ChildItem -Path $directory_name -Include * -File -Recurse | foreach { $_.Delete()}
}

function global:get-dotnet(){
	return (Get-Command dotnet).Path
}
