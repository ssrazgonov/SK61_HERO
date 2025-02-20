using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{
    // Массив позиций зон попадания для каждой полосы
    public Transform[] hitZones;
    // Допустимое расстояние для засчитывания попадания
    public float hitThreshold = 1f;
    // Объекты для визуального выделения полос (например, подсветка)
    public SpriteRenderer[] laneHighlights;
    // Цвет подсветки при нажатии
    public Color highlightColor = Color.white;
    // Обычный цвет полосы
    public Color normalColor = Color.gray;
    // Длительность подсветки
    public float highlightDuration = 0.2f;

    public TMP_Text scoreText;
    public TMP_Text scoreTextEnd;
    public int score = 0;  

    public float hitZoneMoveDistance = 0.2f;      
    public float hitZoneAnimDuration = 0.1f;   

    private Vector3[] basePositions;
    private Coroutine[] activeAnimations;

    void Start() {
        basePositions = new Vector3[hitZones.Length];
        activeAnimations = new Coroutine[hitZones.Length];
        for (int i = 0; i < hitZones.Length; i++)
        {
            basePositions[i] = hitZones[i].position;
        }
    }

    void Update()
    {
        // При нажатии на клавиши для каждой полосы запускается анимация и проверка попадания.
        if (Input.GetKeyDown(KeyCode.A))
        {
            StartCoroutine(AnimateHitZone(0));
            CheckHit(0);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            StartCoroutine(AnimateHitZone(1));
            CheckHit(1);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            StartCoroutine(AnimateHitZone(2));
            CheckHit(2);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(AnimateHitZone(3));
            CheckHit(3);
        }
    }

    // Метод перебирает все активные ноты в сцене и ищет ближайшую ноту для заданной полосы.
    // Если расстояние между нотой и зоной попадания меньше hitThreshold, нота засчитывается.
    void CheckHit(int laneIndex)
    {
        Note closestNote = null;
        float closestDistance = hitThreshold;
        foreach (Note note in FindObjectsOfType<Note>())
        {
            if (note.laneIndex == laneIndex)
            {
                // Считаем расстояние по оси Z (предполагается, что именно она отвечает за движение нот к hitZone)
                float distance = Mathf.Abs(note.transform.position.z - hitZones[laneIndex].position.z);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNote = note;
                }
            }
        }
        if (closestNote != null)
        {
            closestNote.Hit();
            score += 100;
            UpdateScoreUI();
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
            scoreTextEnd.text = score.ToString();
        }
    }

    IEnumerator AnimateHitZone(int laneIndex)
    {
        Vector3 originalPos = basePositions[laneIndex];
        Vector3 targetPos = originalPos + Vector3.down * hitZoneMoveDistance;
        float elapsedTime = 0f;

        while (elapsedTime < hitZoneAnimDuration)
        {
            hitZones[laneIndex].position = Vector3.Lerp(originalPos, targetPos, elapsedTime / hitZoneAnimDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        hitZones[laneIndex].position = targetPos;

        elapsedTime = 0f;
        while (elapsedTime < hitZoneAnimDuration)
        {
            hitZones[laneIndex].position = Vector3.Lerp(targetPos, originalPos, elapsedTime / hitZoneAnimDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        hitZones[laneIndex].position = originalPos;
    }
}
