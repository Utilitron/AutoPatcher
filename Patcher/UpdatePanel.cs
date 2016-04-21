using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class UpdatePanel : MonoBehaviour {
	public Text updateText;

	public void Display(string update) {
		updateText.text = update;

		gameObject.SetActive(true);
	}
}
