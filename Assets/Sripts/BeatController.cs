using UnityEngine;
using System.Collections;
using System;

public class BeatController : MonoBehaviour
{
    public AudioSource song;           // Аудиоисточник для воспроизведения мелодии
    public float bpm = 120f;           // Установленный BPM мелодии
    public GameObject notePrefab;      // Префаб ноты
    public Transform[] lanes;          // Массив позиций для линий, на которых будут появляться ноты
    public float noteSpeed = 5f;       // Скорость движения ноты
    public SpriteRenderer[] laneHighlights;  // Визуальные элементы для подсветки линий
    public Color beatColor = Color.yellow;   // Цвет подсветки на такт
    public Color normalColor = Color.white;  // Исходный цвет линий
    public float highlightDuration = 0.1f;   // Длительность подсветки
    public float skipChance = 0.3f;    // Вероятность пропуска ноты на конкретной линии

    private float spawnInterval;       // Интервал между спавном нот, рассчитывается по BPM
    private float timer;    
    
    public bool started = false;
    
    public GameObject endGamePanel;   
    
    public GameController gameController;        // Таймер для отсчёта до следующего такта

    void Start()
    {
                  
    }

    public void StartGame()
    {   
        
        spawnInterval = 60f / bpm;     // Вычисляем интервал в секундах между ударами
        timer = spawnInterval;         // Инициализируем таймер
        song.Play();   
        endGamePanel.SetActive(false);
        gameController.score = 0;
        StartCoroutine(startBeat());
        
                     // Запускаем мелодию
    }

    void Update()
    {
        if (!started) {
            return;
        }

        if (song.time >= song.clip.length && started) {
            started = false;
            endGamePanel.SetActive(true);
        }

        timer -= Time.deltaTime;       // Отсчитываем время
        if (timer <= 0f)
        {
            SpawnNotes();              // Создаем ноты с учетом вероятности пропуска
            StartCoroutine(HighlightLanes());  // Подсвечиваем линии для визуального эффекта
            timer += spawnInterval;      // Сбрасываем таймер для следующего такта
        }
    }

    void SpawnNotes()
    {
        foreach (Transform lane in lanes)
        {
            if (UnityEngine.Random.value >= skipChance)
            {
                GameObject noteObj = Instantiate(notePrefab, lane.position, Quaternion.identity);
                Note noteScript = noteObj.GetComponent<Note>();
                if (noteScript != null)
                {
                    noteScript.speed = noteSpeed;

                    int laneIndex = Array.IndexOf(lanes, lane);
                    noteScript.laneIndex = laneIndex;
                }
            }
        }
    }

    IEnumerator HighlightLanes()
    {
        if (laneHighlights != null)
        {
            foreach (SpriteRenderer sr in laneHighlights)
            {
                sr.color = beatColor;
            }
            yield return new WaitForSeconds(highlightDuration);
            foreach (SpriteRenderer sr in laneHighlights)
            {
                sr.color = normalColor;
            }
        }
    }

    IEnumerator startBeat()
    {
        yield return new WaitForSeconds(2);
        started = true; 
    }
    
}
