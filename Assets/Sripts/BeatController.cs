using UnityEngine;
using System.Collections;

public class BeatController : MonoBehaviour
{
    public AudioSource song;           // Аудиоисточник для воспроизведения мелодии
    public LevelData levelData;        // Ссылка на данные уровня
    public GameObject notePrefab;      // Префаб ноты
    public Transform[] lanes;          // Массив позиций для линий, на которых будут появляться ноты
    public float noteSpeed = 5f;       // Скорость движения ноты
    public SpriteRenderer[] laneHighlights;  // Визуальные элементы для подсветки линий
    public Color beatColor = Color.yellow;   // Цвет подсветки на такт
    public Color normalColor = Color.white;  // Исходный цвет линий
    public float highlightDuration = 0.1f;   // Длительность подсветки
    public GameObject endGamePanel;   // Панель конца игры

    public float spawnOffsetDistance = 10f;  // Расстояние от точки спавна до hit zone

    private float spawnInterval;       // Интервал между спавном нот, рассчитывается по BPM
    private float timer;               // Таймер для отсчёта до следующего такта

    public bool started = false;       // Флаг начала игры

    public GameController gameController;        // Контроллер игры для отсчёта до следующего такта

    void Start()
    {
        if (levelData != null)
        {
            spawnInterval = 60f / levelData.bpm;  // Вычисляем интервал между ударами
            timer = spawnInterval;  // Инициализируем таймер
            song.clip = levelData.music;  // Устанавливаем музыку
        }
    }

    /// <summary>
    /// Запускаем игру с задержкой, равной времени, за которое нота преодолевает расстояние от спавна до hit zone.
    /// Это позволяет синхронизировать прибытие нот с музыкой.
    /// </summary>
    public void StartGame()
    {
        if (levelData == null) return;

                // Сброс флага spawned для всех нот при запуске игры
        if (levelData.notes != null)
        {
            for (int i = 0; i < levelData.notes.Length; i++)
            {
                levelData.notes[i].spawned = false;
            }
        }

        // Вычисляем время смещения: сколько секунд требуется, чтобы нота прошла расстояние spawnOffsetDistance
        float spawnOffsetTime = spawnOffsetDistance / noteSpeed;
        StartCoroutine(DelayedStart(spawnOffsetTime));
    }

    IEnumerator DelayedStart(float delay)
    {
        // Задержка перед стартом музыки позволяет нотам появиться заранее
        yield return new WaitForSeconds(delay);
        song.Play();
        endGamePanel.SetActive(false);
        gameController.score = 0;
        StartCoroutine(startBeat());
    }

    void Update()
    {
        if (!started) return;

        if (song.time >= song.clip.length && started)
        {
            started = false;
            endGamePanel.SetActive(true);
        }

        timer -= Time.deltaTime;  // Отсчитываем время
        if (timer <= 0f)
        {
            SpawnNotes();  // Создаем ноты из LevelData
            StartCoroutine(HighlightLanes());  // Подсвечиваем линии для визуального эффекта
            timer += spawnInterval;  // Сбрасываем таймер для следующего такта
        }
    }

    /// <summary>
    /// Создает ноты, если текущий момент воспроизведения попадает в промежуток [startTime - spawnOffsetTime, startTime + duration].
    /// Каждая нота создается только один раз.
    /// </summary>
    void SpawnNotes()
    {
        if (levelData.notes == null) return;

        // Вычисляем время смещения: сколько секунд требуется, чтобы нота прошла spawnOffsetDistance
        float spawnOffsetTime = spawnOffsetDistance / noteSpeed;

        // Используем for, чтобы можно было изменять свойство spawned в объекте noteData
        for (int i = 0; i < levelData.notes.Length; i++)
        {
            NoteData noteData = levelData.notes[i];

            // Если нота уже создана, пропускаем её
            if (noteData.spawned)
                continue;

            // Если нота должна быть активной в текущий момент (учитывая появление заранее)
            if (noteData.startTime - spawnOffsetTime <= song.time && noteData.startTime + noteData.duration >= song.time)
            {
                int laneIndex = noteData.trackIndex;
                if (laneIndex >= 0 && laneIndex < lanes.Length)
                {
                    GameObject noteObj = Instantiate(notePrefab, lanes[laneIndex].position, Quaternion.identity);
                    Note noteScript = noteObj.GetComponent<Note>();
                    if (noteScript != null)
                    {
                        noteScript.speed = noteSpeed;
                        noteScript.laneIndex = laneIndex;
                        noteScript.startTime = noteData.startTime;
                        noteScript.duration = noteData.duration;
                    }
                    
                    // Устанавливаем масштаб по оси Z так, чтобы он соответствовал длительности ноты.
                    Vector3 scale = noteObj.transform.localScale;
                    scale.z = noteData.duration;
                    noteObj.transform.localScale = scale;
                }
                // Помечаем ноту как созданную, чтобы избежать её повторного спавна
                noteData.spawned = true;
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
        yield return new WaitForSeconds(levelData.startDelay);
        started = true;
    }
}
