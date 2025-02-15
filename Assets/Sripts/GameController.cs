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
        // Обрабатываем нажатия клавиш для каждой полосы (A, S, D, F)
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

    // Метод проверки попадания по ноте в заданной полосе
    void CheckHit(int laneIndex)
    {
        Note closestNote = null;
        float closestDistance = hitThreshold;
        // Перебираем все активные ноты
        foreach (Note note in FindObjectsOfType<Note>())
        {
            Debug.Log(note.laneIndex + "линий ноты");
            if (note.laneIndex == laneIndex)
            {
                
                // Вычисляем расстояние между нотой и зоной попадания по оси Y
                float distance = Mathf.Abs(note.transform.position.z - hitZones[laneIndex].position.z);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNote = note;
                }
            }
        }
        // Если нота найдена и находится в пределах допустимого расстояния, засчитываем попадание
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
            scoreText.text = "" + score;
            scoreTextEnd.text = "" + score;
        }
    }

    void AnimateLane(int laneIndex)
    {
        if (activeAnimations[laneIndex] != null)
        {
            StopCoroutine(activeAnimations[laneIndex]);
            hitZones[laneIndex].position = basePositions[laneIndex];
        }
        activeAnimations[laneIndex] = StartCoroutine(AnimateHitZone(laneIndex));
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
        activeAnimations[laneIndex] = null;
    }
}
