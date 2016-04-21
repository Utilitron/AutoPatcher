using UnityEngine;

using System;
using System.Collections;
using System.IO;

public class Updater : MonoBehaviour {
	public UpdatePanel updatePanel;

	private string patcherURL = "http://website.com/patcher";
	private string gameURL = "http://website.com/game";

	private ArrayList downloadList = new ArrayList();

	private void Start() {
		var _this = this;
		StartCoroutine(CheckForGameUpdates ((updateRequired) => { 
			_this.AfterUpdateGameCheck(updateRequired);
		}));
	}

	public void AfterUpdateGameCheck(bool gameUpdateRequired) {
		updatePanel.gameObject.SetActive(false);
		Debug.Log ("AfterUpdateGameCheck: " + gameUpdateRequired);
		if (gameUpdateRequired) {
			var _this = this;
			StartCoroutine(CheckPatcherUpdates ((updateRequired) => { 
				_this.AfterUpdatePatcherCheck(updateRequired);
			}));
		} else {
			GameObject.Find ("PatchPanel").SetActive(false);
			ChangeTo(mainMenuPanel);
		}	
	}

	public void AfterUpdatePatcherCheck(bool updateRequired) {
		Debug.Log ("AfterUpdatePatcherCheck: " + updateRequired);
		updatePanel.gameObject.SetActive(false);
		if (updateRequired) {
			StartCoroutine(ApplyPatcherUpdate ());
		} else {
			ApplyGameUpdate ();
		}	
	}


	public IEnumerator CheckForGameUpdates(System.Action<bool> callback) {
		bool updateRequired = false;

		string buildVersion = "";
		try {
			buildVersion = System.IO.File.ReadAllText (Directory.GetCurrentDirectory() + "/version.txt");
		} catch (Exception e) {
			buildVersion = "0.0.0";
			Console.WriteLine("{0}\n", e.Message);
		}

		updatePanel.Display ("Checking For Updates...");
		Debug.Log ("Checking For Updates...");

		WWW version_get = new WWW(gameURL + "/version.txt");
		yield return version_get;

		if (version_get.error != null) {
			updatePanel.Display ("There was an error getting the version: \n" + version_get.error);
			Debug.Log ("There was an error getting the version: " + version_get.error);
		} else {
			string updateVersion = (version_get.text).Trim();
			Debug.Log ("Game updateVersion " + updateVersion + " buildVersion " + buildVersion.Trim());
			if (updateVersion.Trim() == buildVersion.Trim()){
				updatePanel.Display ("Currently update to date.");
				Debug.Log ("Currently update to date.");
			} else {
				updateRequired = true;
			}
		}

		callback(updateRequired);
	}

	public void ApplyGameUpdate(){
		Debug.Log ("ApplyGameUpdate");
		string extension = "Patcher.exe";
		if(Application.platform == RuntimePlatform.OSXPlayer){
			extension = "Patcher.app";
		} else if(Application.platform == RuntimePlatform.LinuxPlayer){
			extension = "Patcher.x86_64";
		}
		Debug.Log (Directory.GetCurrentDirectory() + "/" + Application.productName.Replace("Patcher","") + extension);
		System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo (
			Directory.GetCurrentDirectory() + "/" + Application.productName.Replace("Patcher","") + extension, 
			"-logFile patch_output.log")
			{ UseShellExecute = false });

		Application.Quit();
	}

	public IEnumerator CheckPatcherUpdates(System.Action<bool> callback) {
		bool updateRequired = false;

		string buildVersion = "";
		try {
			buildVersion = System.IO.File.ReadAllText (Directory.GetCurrentDirectory() + "/patch_version.txt");
		} catch (Exception e) {
			buildVersion = "0.0.0";
			Console.WriteLine("{0}\n", e.Message);
		}

		updatePanel.Display ("Checking For Updates...");
		Debug.Log ("Checking For Updates...");

		WWW version_get = new WWW(patcherURL + "/patch_version.txt");
		yield return version_get;

		if (version_get.error != null) {
			updatePanel.Display ("There was an error getting the version: " + version_get.error);
			Debug.Log ("There was an error getting the version: " + version_get.error);
		} else {
			string updateVersion = (version_get.text).Trim();
			Debug.Log ("Patcher updateVersion " + updateVersion + " buildVersion " + buildVersion.Trim());
			if (updateVersion.Trim() != buildVersion.Trim()){
				updateRequired = true;
			}
		}

		callback(updateRequired);
	}

	public IEnumerator ApplyPatcherUpdate(){
		Debug.Log ("ApplyPatcherUpdate");
		WWW version_get = new WWW(patcherURL + "/patch_version.txt");
		yield return version_get;

		if (version_get.error != null) {
			updatePanel.Display ("There was an error getting the version: " + version_get.error);
			Debug.Log ("There was an error getting the version: " + version_get.error);
		} else {
			string updateVersion = (version_get.text).Trim();
			Debug.Log(patcherURL + "/" + updateVersion.Replace(".","_").Trim() + "/" + Application.platform.ToString() + "/" + "patch.txt");

			//open and read the patch file contents
			WWW patch_get = new WWW(patcherURL + "/" + updateVersion.Replace(".","_").Trim() + "/" + Application.platform.ToString() + "/" + "patch.txt");
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

			StartCoroutine (GetNextFile (0, updateVersion));
		}
	}

	private IEnumerator GetNextFile(int currentIndex, string updateVersion) {
		if (downloadList.Count > currentIndex) {
			var fileName = downloadList[currentIndex].ToString().Trim();
			updatePanel.Display ("File "+(currentIndex+1)+" of " + downloadList.Count + "\n\nDownloading " + fileName);
			Debug.Log("File "+(currentIndex+1)+" of " + downloadList.Count + "\n\nDownloading " + fileName);
			Debug.Log("URL: " + patcherURL + "/" + updateVersion.Replace(".","_").Trim() + "/" + Application.platform.ToString() + "/" + fileName);

			WWW file_get = new WWW(patcherURL + "/" + updateVersion.Replace(".","_").Trim() + "/" + Application.platform.ToString() + "/" + fileName);
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
					System.IO.File.WriteAllBytes (Directory.GetCurrentDirectory() + fileName, file_get.bytes);
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
			System.IO.File.WriteAllText (Directory.GetCurrentDirectory() + "/patch_version.txt", updateVersion);
			ApplyGameUpdate ();
		}
	}
}
