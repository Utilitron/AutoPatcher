using UnityEngine;
using System;
using System.Collections;
using System.IO;

public class Updater : MonoBehaviour {
	public UpdatePanel updatePanel;

	private string gameURL = "http://website.com/game";

	private ArrayList downloadList = new ArrayList();

	private void Start() {
		Debug.Log ("Start");
		StartCoroutine (ApplyGameUpdate ());
	}

	public IEnumerator ApplyGameUpdate(){
		Debug.Log ("ApplyGameUpdate");
		WWW version_get = new WWW(gameURL + "/version.txt");
		yield return version_get;

		if (version_get.error != null) {
			updatePanel.Display ("There was an error getting the version: " + version_get.error);
			Debug.Log ("There was an error getting the version: " + version_get.error);
		} else {
			string updateVersion = (version_get.text).Trim();
			Debug.Log(gameURL + "/" + updateVersion.Replace(".","_").Trim() + "/" + Application.platform.ToString() + "/" + "patch.txt");

			//open and read the patch file contents
			WWW patch_get = new WWW(gameURL + "/" + updateVersion.Replace(".","_").Trim() + "/" + Application.platform.ToString() + "/" + "patch.txt");
			yield return patch_get;

			string fileList = patch_get.text;
			string[] fileListArray = fileList.Split("\n" [0]);    //every single line in patch.txt seperated into an array
			Debug.Log("Download List:");
			for (var j = 0; j < fileListArray.Length; j++) {
				if (fileListArray [j].Trim () != "") {
					downloadList.Add (fileListArray [j].Trim ());
					Debug.Log (fileListArray [j].Trim ());
				}
			}

			//generate the list of files to download (removing duplicates)
			//int convertedString;
			//bool addFiles = false;
			//bool found = false;

			//for(var j =0; j < fileListArray.Length; j++){ //Look through list and build files to download list
			//    if(fileListArray[j].Trim() == "patch"){
			//		convertedString = int.Parse(fileListArray[(j+1)].Replace(".","").Trim());
			//		if(convertedString > int.Parse(buildVersion)) {
			//		  addFiles = true;
			//		  j = j + 2;
			//	   }
			//    }

			//    if(addFiles == true){
			//	   for(var k = 0; k < downloadList.Count; k++) {
			//		  if(downloadList[k] == fileListArray[j].Trim()){
			//			 found = true;
			//			 break;
			//		  }
			//	   }
			//	   if(found == false)
			//		  downloadList.Add(fileListArray[j].Trim());
			//    }
			//    found = false;
			//}

			StartCoroutine (GetNextFile (0, updateVersion));
		}
	}

	private IEnumerator GetNextFile(int currentIndex, string updateVersion) {
		if (downloadList.Count > currentIndex) {
			var fileName = downloadList[currentIndex].ToString().Trim();
			updatePanel.Display ("File "+(currentIndex+1)+" of " + downloadList.Count + "\n\nDownloading " + fileName);
			Debug.Log("File "+(currentIndex+1)+" of " + downloadList.Count + "\n\nDownloading " + fileName);
			Debug.Log("URL: " + gameURL + "/" + updateVersion.Replace(".","_").Trim() + "/" + Application.platform.ToString() + "/" + fileName);

			WWW file_get = new WWW(gameURL + "/" + updateVersion.Replace(".","_").Trim() + "/" + Application.platform.ToString() + "/" + fileName);
			yield return file_get;

			if (file_get.error != null) {
				updatePanel.Display ("There was an error getting the file: " + file_get.error);
				Debug.Log ("There was an error getting the file: " + file_get.error);
			} else {
				while (!file_get.isDone) {
					updatePanel.Display ("Download Progress: " + (file_get.progress * 100).ToString ("##0.00") + "%");
					Debug.Log("Download Progress: " + (file_get.progress * 100).ToString ("##0.00") + "%");
				}
			}

			try {
				updatePanel.Display ("...saving...");
				Debug.Log("...saving...");
				if(Application.platform == RuntimePlatform.OSXPlayer){
					System.IO.File.WriteAllBytes (Directory.GetCurrentDirectory() + "/" + fileName, file_get.bytes);
				}
				else if(Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.LinuxPlayer){
					System.IO.File.WriteAllBytes (Directory.GetCurrentDirectory() + "/" + fileName, file_get.bytes);
				} 

			} catch(Exception e){
				updatePanel.Display ("Update Failed with error message:\n\n"+e.ToString());
				Console.WriteLine("{0}\n", e.Message);
			}

			StartCoroutine (GetNextFile (currentIndex+1, updateVersion));
		} else {
			System.IO.File.WriteAllText (Directory.GetCurrentDirectory() + "/version.txt", updateVersion);
			LaunchGame ();
		}
	}

	public void LaunchGame (){
		string extension = ".exe";
		if(Application.platform == RuntimePlatform.OSXPlayer){
			extension = ".app";
		} else if(Application.platform == RuntimePlatform.LinuxPlayer){
			extension = ".x86_64";
		}
		Debug.Log (Directory.GetCurrentDirectory() + "/" + Application.productName.Replace("Patcher","") + extension);
		System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo (
			Directory.GetCurrentDirectory() + "/" + Application.productName.Replace("Patcher","") + extension, 
			"-logFile output.log")
			{ UseShellExecute = false });
		
		Application.Quit();
	}
}
