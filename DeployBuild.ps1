
param (
    [Parameter()]
    [string]$Name
)
[xml]$xml = Get-Content "C:\Devops\BBY.packageops\BBYPOS.Git.main\Install\Variables.xml"
$RtpPath = $xml.Variables.Variable | Where-Object { $_.Name -eq "POSBranch" } | Select-Object -ExpandProperty "#text"

$extractPath = "C:\Devops\BBY.packageops\BBYPOS.Git.main\Install\temp.extracts"
$posBuildPath = "C:\BBYApps\E3Retail\PointOfService"
$retry=0
#$rtpBldpath= "\\rtp-dc1-svr.corp.e3retail.com\tc-releases\BBY-BestBuy\main"
$rtpBldpath="\\rtp-dc1-svr.corp.e3retail.com\tc-releases\"+$RtpPath
# Define function to get current date and time
function GetDateTime {
    return Get-Date -Format "yyyy-MM-dd HH:mm:ss"
}

function download {

	 AppendText "Downloading"
        ConnectRTP
        .\package.ps1 -download
		if (!(Test-Path $extractPath) -and ($retry -lt 2)) {
			download
			$retry = $retry + 1
}
 elseif ($retry -ge 2) {
        throw "Download failed after 2 retries."
    }	
		Copy-Item "C:\Users\Administrator\Desktop\25.3\Encryp\E3Retail.TP.EncryptionService.WindowsService.exe.config" -Destination "C:\Program Files (x86)\BBYApps\E3Retail\Services\Encryption"
	
        DisconnectRTP
}

# Define the file path
$FilePath = "C:\DeployBuildLogs.txt"
  Set-Location -Path "C:\Devops\BBY.packageops\BBYPOS.Git.main\Install"
# Placeholder function for connecting to RTP
function ConnectRTP {  
 #   appendText "Disconnecting RTP2BBY"

rasdial.exe RTP2BBY /disconnect
#rasdial.exe "RTP TO BBY" /disconnect
#appendText "Connecting RTP"
rasdial RTP
}

# Placeholder function for disconnecting from RTP
function DisconnectRTP {
  
#   appendText "Disconnecting RTP"

rasdial.exe RTP /disconnect
#appendText "Connecting RTP2BBY"
rasdial "RTP2BBY"
}

# The rest of your script goes here...


# Define function to append text to a file and display it on console
function AppendText {
    param(
        [string]$Text
    )

    $TextToAppend = (GetDateTime) + " " + $Text
    Add-Content -Path $FilePath -Value $TextToAppend
    Write-Host $TextToAppend
}

function Update { 
    param(
        [string]$keyToUpdate, 
        [string]$newValue
    )   

    $node = $xml.configuration.appSettings.add | Where-Object { $_.key -eq $keyToUpdate }
    
    if ($node) {
        $node.value = $newValue
        Write-Host "Updated '$keyToUpdate' to '$newValue'"
    } else {
        Write-Host "Key '$keyToUpdate' not found!"
    }
}

function UpdatePartial {
    param(
        [string]$keyToUpdate, 
        [string]$oldValue,
        [string]$newValue
    )   

    $node = $xml.configuration.appSettings.add | Where-Object { $_.key -eq $keyToUpdate }

    if ($node) {
        $node | ForEach-Object {
            $_.value = $_.value -replace $oldValue, $newValue
        }
        Write-Host "Updated '$keyToUpdate' replacing '$oldValue' with '$newValue'"
    } else {
        Write-Host "Key '$keyToUpdate' not found!"
    }
}

function UpdateScript{

   # Update DMS-PORT
        $xmlFilePath = "C:\Program Files (x86)\BBYApps\E3Retail\PointOfService\E3Retail.CloudPos.Host.dll.config"
        $xml = [xml](Get-Content $xmlFilePath)

        # Update the port number
      UpdatePartial "E3Retail.DMS.HTTP.DMSServiceHttpEndpoint" "5555" "5551"

        # Update P2P URL
      Update "ServiceUrlForP2PRelationshipForItem" "http://del-app2-svr:8085/api/json/itemrelationshipdata"

        # Update Pay By Link START





		Update "TAPServiceTokenServiceKerberosAuth" "false"         
		
		# Update the CDSServiceUrlForGetPlansAndSubscriptions		
		
		Update "CDSServiceUrlForGetPlansAndSubscriptions" "https://apid-int.test.bestbuy.com/gw/plans-on-profile-secure/plans"
        
		# Update the CreditCardTokenREST
		
		
        # Update the port number
		UpdatePartial "TGServiceTokenServiceURL" "8990" "8080"
		 
		 # Update PartnerSettingsAPIKerberosAuth
		Update "PartnerSettingsAPIKerberosAuth" "false"
        # Save the updated XML file
        $xml.Save($xmlFilePath)

        Write-Host "Port number updated to 5551."
        Write-Host "P2P URL updated to http://del-app2-svr:8085/api/json/itemrelationshipdata"
        Write-Host "http://dtw01ps2wb01c.na.bestbuy.com:8080/gw/transaction-graph/bestbuy/v2/transactions"
		Write-Host "CDSServiceUrlForGetPlansAndSubscriptions updated"
		Write-Host "CreditCardTokenREST Url updated"

        # Stop IIS PointOfService
        Stop-WebSite -Name PointOfService

        # Start IIS PointOfService
        Start-WebSite -Name PointOfService
}

# Output initial message
AppendText "Starting script execution"

# Check if parameter is provided
if (-not $PSBoundParameters.ContainsKey('Name')) {
    Write-Host "Parameter is not provided"

    # Output message and connect to RTP
   
    ConnectRTP

    # Get the most recently updated folder
    $recentFolder = Get-ChildItem -Path $rtpBldpath -Directory | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 1

    $recentFolderName = $recentFolder.Name
    $lastWriteTime = $recentFolder.LastWriteTime
    AppendText "The recently updated folder is '$recentFolderName' and it was last updated on $lastWriteTime."

    # Construct the path to the latest build
    $path = "C:\Devops\BBY.packageops\BBYPOS.Git.main\Install\temp.downloads\BBY.POS\$recentFolderName"

    # Check if the latest build is already deployed
  if ((Test-Path $path) -and (Test-Path $extractPath) -and (Test-Path $posBuildPath) ) {
        AppendText "Latest build $recentFolderName already deployed"
        DisconnectRTP
    } else {
        # Deployment steps
        try {
            AppendText "Deploying Build $recentFolderName"

            # Step 1: Uninstall
            AppendText "Uninstalling"
            .\package.ps1 -uninstall

            # Step 2: Download
				download

            # Step 3: Install
            AppendText "Installing"

            #command for stopping service
            Set-Service -Name "E3EncryptionService" -Status stopped
            .\package.ps1 -install

            # Update script
            UpdateScript

            # Disconnect from RTP
            DisconnectRTP

            #Reset App-Pool
            Stop-WebAppPool -Name "PointOfService"
            Start-Sleep -Seconds 15
            Start-WebAppPool -Name "PointOfService"

            AppendText "Latest Build '$recentFolderName' deployed at $env:COMPUTERNAME."
        } catch {
            AppendText "An error occurred: $_"
        }
    }
} else {
    # Execute actions based on provided parameter
    if($Name.Contains('u')) {
        AppendText "Uninstalling"
        .\package.ps1 -uninstall
    }

    if($Name.Contains('d')) {
       download
    }

    if($Name.Contains('i')) {
        AppendText "Installing"
        Set-Service -Name "E3EncryptionService" -Status stopped
        .\package.ps1 -install
    }

    if($Name.Contains('s')) {
        AppendText "Updating Script"
        UpdateScript
    }
}
