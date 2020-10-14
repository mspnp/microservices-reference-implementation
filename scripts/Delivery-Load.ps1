#  ------------------------------------------------------------
#   Copyright (c) Microsoft Corporation.  All rights reserved.
#   Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
#  ------------------------------------------------------------

# Accepts the baseUrl as an argument and if not passed default testing URL will be triggred.
if($args.count -gt 0 )
{
	$baseUrl = $args[0]
}
else
{
	# For testing use "https://russdronebasic2-ingest-dev.eastus.cloudapp.azure.com"
	# This needs to be changed going forward
	$baseUrl = "https://russdronebasic2-ingest-dev.eastus.cloudapp.azure.com"
}

$ProgressPreference = "SilentlyContinue"
$deliveryRequestEndPoint= "/api/deliveryrequests"
$deliveryStatusEndPoint= "/api/deliveries/{0}/status"
$delayForRequests = 1
[int]$requestLatencyMedium = 150
[int]$requestLatencyHigh = 200
$psVersion = $PSVersionTable.PSEdition
if ($psVersion -eq 'Core') {
    $requestLatencyMedium = 250
    $requestLatencyHigh = 270
}

$header = @{
 "Accept"="application/json"
 "Content-Type"="application/json"
}

#Json that need to be posted to the delivery requests.
$jsonToPost = @'
{
   "confirmationRequired": "None",
   "deadline": "",
   "dropOffLocation": "drop off1",
   "expedited": true,
   "ownerId": "myowner",
   "packageInfo": {
     "packageId": "mypackage",
     "size": "Small",
     "tag": "mytag",
     "weight": 10
   },
   "pickupLocation": "my pickup1",
   "pickupTime": "2019-05-08T20:00:00.000Z"
 }
'@

if ($psVersion -ne 'Core') {
    try {
  add-type @"
     using System.Net;
     using System.Security.Cryptography.X509Certificates;
     public class TrustAllCertsPolicy : ICertificatePolicy {
     public bool CheckValidationResult(
        ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
     }
  }
"@
    } catch {
        
    }
 [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
}

	try
	{
        Write-Host "Requesting Tracking ID: " -NoNewline

        #Calls the delivery request API and fetches the tracking number
        if ($psVersion -eq 'Core') {
		    $response = Invoke-WebRequest -Uri ($baseUrl + $deliveryRequestEndPoint) -Method POST -Body $jsonToPost -Headers $header -SkipCertificateCheck
        } else {
            $response = Invoke-WebRequest -Uri ($baseUrl + $deliveryRequestEndPoint) -Method POST -Body $jsonToPost -Headers $header
        }

		#Response with StatusCode = 202 is only treated as a Valid request, rest will be treated as Invalid
		if($response.StatusCode -eq "202")
		{
			$trackingId = $response.Content | ConvertFrom-Json | Select-Object deliveryId

            Write-Host "$($trackingId.deliveryId)"
            Write-Host
		
            Write-Host "Waiting for request to process..." -NoNewline
            Start-Sleep -Seconds 2

            Write-Host
            Write-Host "Requesting Delivery Status..."
            Write-Host

			while ($true)
			{
				$statusUrl= ($baseUrl + $deliveryStatusEndPoint) -f $trackingId.deliveryId

                Write-Host "Delivery Status - Request Time(ms): " -NoNewline

				try
				{
					#Calls the status API continously
                    [int]$responseTime = 0;
                    
                    if ($psVersion -eq 'Core') {
                        $responseTime = (Measure-Command -Expression {Invoke-WebRequest -Uri $statusUrl -Method GET -SkipCertificateCheck}).Milliseconds 
                    } else {
                        $responseTime = (Measure-Command -Expression {Invoke-WebRequest -Uri $statusUrl -Method GET }).Milliseconds
                    }
				}
				catch
				{
					Write-Output "Error while checking status"
				}

                $fgColor = "White"
                $bgColor = "DarkGreen"

                if ($responseTime -gt $requestLatencyHigh) {
                    $fgColor = "White"
                    $bgColor = "Red"
                } elseif ($responseTime -gt $requestLatencyMedium) {
                    $fgColor = "Black"
                    $bgColor = "Yellow"
                } else {
                    $fgColor = "White"
                    $bgColor = "DarkGreen"
                }

                Write-Host "  $responseTime" -ForegroundColor $fgColor -BackgroundColor $bgColor -NoNewline 
                for ($i=0; $i -lt $responseTime / 10; $i++) { Write-Host "_" -ForegroundColor $bgColor -BackgroundColor $bgColor -NoNewline }
                Write-Host

				Start-Sleep -Seconds $delayForRequests
			}
		}
		else
		{
			Write-Output "Error Requesting Tracking ID:" + $response.StatusCode + " " + $response.StatusDescription
		}
	}
	catch
	{
		Write-Output "Fatal Error: $_"
	}
