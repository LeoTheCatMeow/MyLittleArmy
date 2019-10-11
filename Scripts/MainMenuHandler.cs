using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuHandler : MonoBehaviour {

	public GameObject mainMenu;
	public GameObject loadingText;
	public Sprite[] levelSSCollection;
	public string[] levelNames;
	public Image levelSS;
	public Text levelName;
	public Text soundStatus; 
	public AudioClip onClickSound;
	private GameObject currentMenu;
	private int currentLevel;
	private bool soundEnabled = true; 
	private bool inAnimation;

	void Start () {
		currentMenu = mainMenu;
		currentLevel = 0;
	}

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void Goto (GameObject go) {
		if (!inAnimation) {
			GetComponent<AudioSource> ().PlayOneShot (onClickSound, 1f);
			StartCoroutine (_Goto (go));
		}
	}

	private IEnumerator _Goto (GameObject go) {
		inAnimation = true;
		currentMenu.GetComponent<Animator> ().Play ("Close");
		yield return new WaitForSeconds (0.75f);
		currentMenu.SetActive (false);
		currentMenu = go; 
		go.SetActive (true);
		inAnimation = false; 
	}

	public void NextLevel () {
		if (currentLevel < levelNames.Length - 1) {
			currentLevel++;
			levelSS.sprite = levelSSCollection [currentLevel];
			levelName.text = levelNames [currentLevel];
		}
		GetComponent<AudioSource> ().PlayOneShot (onClickSound, 1f);
	}

	public void PreviousLevel () {
		if (currentLevel > 0) {
			currentLevel--;
			levelSS.sprite = levelSSCollection [currentLevel];
			levelName.text = levelNames [currentLevel];
		}
		GetComponent<AudioSource> ().PlayOneShot (onClickSound, 1f);
	}

	public void Enable_DisableSound () {
		if (soundEnabled) {
			soundEnabled = false;
			soundStatus.text = "Disabled";
			AudioListener.volume = 0f; 
		} else {
			soundEnabled = true; 
			soundStatus.text = "Enabled"; 
			AudioListener.volume = 1f;
		}
		GetComponent<AudioSource> ().PlayOneShot (onClickSound, 1f);
	}

	public void Load () {
		StartCoroutine (_Load ());
	}

	public IEnumerator _Load () {
		currentMenu.GetComponent<Animator> ().Play ("Close");
		yield return new WaitForSeconds (0.75f);
		currentMenu.SetActive (false);
		loadingText.SetActive (true);
		AsyncOperation AsyncLoad = SceneManager.LoadSceneAsync (levelNames [currentLevel]);
		while (!AsyncLoad.isDone) {
			yield return null;
		}
	}
}
