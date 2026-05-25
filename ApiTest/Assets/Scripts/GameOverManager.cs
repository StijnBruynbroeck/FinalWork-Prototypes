using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [Header("UI Elementen")]
    public GameObject gameOverScherm;

    private bool isGameOver = false;

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("GAME OVER! Speler is betrapt.");

        if (gameOverScherm != null)
            gameOverScherm.SetActive(true);

        Time.timeScale = 0f;
        StartCoroutine(HerlaadScene());
    }

    private IEnumerator HerlaadScene()
    {
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}