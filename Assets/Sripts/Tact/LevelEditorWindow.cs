#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

public class LevelEditorWindow : EditorWindow
{
    private LevelData levelData;
    private AudioSource previewSource;
    private float timelinePosition;
    private bool isPlaying;
    private Vector2 scrollPosition; // Позиция горизонтального скролла

    // Параметры таймлайна
    private int trackCount = 1;               // Изначальное количество дорожек
    private const float trackHeight = 50f;      // Высота каждой дорожки (в пикселях)
    private const float timeScale = 100f;       // Масштаб: пикселей на секунду

    // Перетаскивание и изменение размера ноты
    private int draggingNoteIndex = -1;       // -1 означает, что нота не перетаскивается
    private bool isResizing = false;          // Режим изменения длительности ноты
    private Vector2 dragOffset = Vector2.zero;// Смещение курсора внутри ноты при начале перетаскивания
    private const float resizeMargin = 5f;      // Область от правого края ноты для начала изменения размера

    [MenuItem("Window/Rhythm Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("Level Editor");
    }

    void OnGUI()
    {
        // Выбор объекта LevelData
        levelData = (LevelData)EditorGUILayout.ObjectField("Level Data", levelData, typeof(LevelData), false);
        if (levelData == null)
            return;

        // Контролы воспроизведения и управления дорожками
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Play/Pause"))
            TogglePlayback();

        if (GUILayout.Button("Добавить дорожку"))
            trackCount++;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Количество дорожек: " + trackCount);

        if (levelData.music != null)
            timelinePosition = EditorGUILayout.Slider("Time", timelinePosition, 0, levelData.music.length);

        // Размеры таймлайна
        float timelineWidth = (levelData.music != null) ? levelData.music.length * timeScale : position.width - 20;
        float headerHeight = 20; // Высота заголовка с таймлайном
        float timelineHeight = trackCount * trackHeight;

        // Отрисовка заголовка таймлайна с синхронизацией горизонтального скролла (используем scrollPosition.x)
        Rect timelineHeaderRect = GUILayoutUtility.GetRect(timelineWidth, headerHeight);
        DrawSongTimelineHeader(timelineHeaderRect, scrollPosition.x);

        GUILayout.Label("Timeline (левый клик: добавление/перемещение/изменение размера, правый клик: удаление)");

        // Оборачиваем область дорожек и нот в scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(timelineHeight + headerHeight + 20));
        Rect timelineRect = GUILayoutUtility.GetRect(timelineWidth, timelineHeight);

        DrawTimelineBackground(timelineRect);
        DrawNotes(timelineRect);
        DrawTimelineCursor(timelineRect);  // Отрисовка вертикальной полоски на дорожках
        HandleNoteEvents(timelineRect);

        EditorGUILayout.EndScrollView();

        // Обновление позиции воспроизведения, если музыка играет
        if (previewSource != null && previewSource.isPlaying)
        {
            timelinePosition = previewSource.time;
            Repaint();
        }

        // Настройки ритма
        if (levelData != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rhythm Settings", EditorStyles.boldLabel);
            levelData.bpm = EditorGUILayout.FloatField("BPM", levelData.bpm);
            levelData.beatsPerBar = EditorGUILayout.IntField("Beats Per Bar", levelData.beatsPerBar);
            levelData.subdivisions = EditorGUILayout.IntField("Subdivisions", levelData.subdivisions);
            levelData.audioOffset = EditorGUILayout.FloatField("Audio Offset", levelData.audioOffset);
        }
    }

    /// <summary>
    /// Отрисовывает заголовок таймлайна, где деления времени синхронизированы по горизонтали с областью дорожек.
    /// </summary>
    void DrawSongTimelineHeader(Rect headerRect, float horizontalOffset)
    {
        // Фон заголовка
        EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.15f, 0.15f));

        // Интервал делений, например, каждые 1 секунду
        float interval = 1f;
        float totalTime = (levelData.music != null) ? levelData.music.length : 60f;

        // Используем BeginGroup, чтобы сместить отрисовку в зависимости от горизонтального скролла
        GUI.BeginGroup(headerRect);
        for (float t = 0; t < totalTime; t += interval)
        {
            float xPos = t * timeScale - horizontalOffset;
            // Если деление выходит за пределы видимой области, пропускаем его
            if (xPos < 0 || xPos > headerRect.width)
                continue;

            // Вертикальная линия деления
            Handles.color = Color.white;
            Handles.DrawLine(new Vector3(xPos, 0), new Vector3(xPos, headerRect.height));

            // Метка времени
            GUI.Label(new Rect(xPos + 2, 0, 30, headerRect.height), t.ToString("F0") + "s");
        }

        // Отрисовка вертикального курсора текущей позиции
        float currentX = timelinePosition * timeScale - horizontalOffset;
        if (currentX >= 0 && currentX <= headerRect.width)
        {
            Handles.color = Color.red;
            Handles.DrawLine(new Vector3(currentX, 0), new Vector3(currentX, headerRect.height));
        }
        GUI.EndGroup();
    }

    /// <summary>
    /// Отрисовывает фон дорожек, ритмические метки и горизонтальные линии.
    /// При отрисовке вертикальных линий (тактов и долей) применяется смещение, указанное в audioOffset.
    /// </summary>
    void DrawTimelineBackground(Rect timelineRect)
    {
        // Фон таймлайна
        EditorGUI.DrawRect(timelineRect, new Color(0.2f, 0.2f, 0.2f));

        // Дорожки с чередующимися оттенками
        for (int i = 0; i < trackCount; i++)
        {
            Rect laneRect = new Rect(timelineRect.x, timelineRect.y + i * trackHeight, timelineRect.width, trackHeight);
            Color laneColor = (i % 2 == 0) ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.35f, 0.35f, 0.35f);
            EditorGUI.DrawRect(laneRect, laneColor);

            // Границы дорожки
            Handles.color = Color.black;
            Handles.DrawLine(new Vector3(laneRect.x, laneRect.y), new Vector3(laneRect.x + laneRect.width, laneRect.y));
            Handles.DrawLine(new Vector3(laneRect.x, laneRect.y + laneRect.height), new Vector3(laneRect.x + laneRect.width, laneRect.y + laneRect.height));
        }

        // Ритмические метки (вертикальные линии) с учетом audioOffset
        if (levelData.bpm > 0 && levelData.beatsPerBar > 0)
        {
            float secondsPerBeat = 60f / levelData.bpm;
            float secondsPerSubdivision = secondsPerBeat / levelData.subdivisions;
            float totalTime = (levelData.music != null) ? levelData.music.length : 60f;
            // Эффективное время для отрисовки линий (начинаем с audioOffset)
            float effectiveTotalTime = totalTime - levelData.audioOffset;
            int totalSubdivisions = Mathf.CeilToInt(effectiveTotalTime / secondsPerSubdivision);

            for (int i = 0; i < totalSubdivisions; i++)
            {
                // Расчет времени для линии: первая линия начинается в audioOffset
                float gridTime = levelData.audioOffset + i * secondsPerSubdivision;
                float xPos = timelineRect.x + gridTime * timeScale;

                bool isBar = (i % (levelData.beatsPerBar * levelData.subdivisions)) == 0;
                bool isBeat = (i % levelData.subdivisions) == 0;
                
                Color lineColor = isBar ? Color.yellow : isBeat ? Color.red : new Color(0.5f, 0.5f, 0.5f, 0.3f);
                float lineHeight = timelineRect.height;
                float lineWidth = isBar ? 2f : 1f;

                Handles.color = lineColor;
                Vector2 start = new Vector2(xPos, timelineRect.y);
                Vector2 end = new Vector2(xPos, timelineRect.y + lineHeight);
                Handles.DrawLine(start, end, lineWidth);
            }
        }
    }

    /// <summary>
    /// Отрисовывает ноты на дорожках.
    /// </summary>
    void DrawNotes(Rect timelineRect)
    {
        if (levelData.notes != null)
        {
            for (int i = 0; i < levelData.notes.Length; i++)
            {
                NoteData note = levelData.notes[i];
                // Если номер дорожки невалидный, пропускаем ноту
                if (note.trackIndex < 0 || note.trackIndex >= trackCount)
                    continue;

                float noteX = timelineRect.x + note.startTime * timeScale;
                float laneY = timelineRect.y + note.trackIndex * trackHeight;
                float noteWidth = Mathf.Max(note.duration * timeScale, 10f);
                Rect noteRect = new Rect(noteX, laneY + 2, noteWidth, trackHeight - 4);

                EditorGUI.DrawRect(noteRect, Color.green);

                // Рамка ноты
                Handles.color = Color.black;
                Handles.DrawAAPolyLine(2f, new Vector3(noteRect.x, noteRect.y), new Vector3(noteRect.x + noteRect.width, noteRect.y));
                Handles.DrawAAPolyLine(2f, new Vector3(noteRect.x, noteRect.y + noteRect.height), new Vector3(noteRect.x + noteRect.width, noteRect.y + noteRect.height));
                Handles.DrawAAPolyLine(2f, new Vector3(noteRect.x, noteRect.y), new Vector3(noteRect.x, noteRect.y + noteRect.height));
                Handles.DrawAAPolyLine(2f, new Vector3(noteRect.x + noteRect.width, noteRect.y), new Vector3(noteRect.x + noteRect.width, noteRect.y + noteRect.height));
            }
        }
    }

    /// <summary>
    /// Отрисовывает вертикальную полосу, показывающую текущую позицию (курсора) на дорожках.
    /// </summary>
    void DrawTimelineCursor(Rect timelineRect)
    {
        float cursorX = timelineRect.x + timelinePosition * timeScale;
        if (cursorX >= timelineRect.x && cursorX <= timelineRect.x + timelineRect.width)
        {
            Handles.color = Color.red;
            Handles.DrawLine(new Vector3(cursorX, timelineRect.y), new Vector3(cursorX, timelineRect.y + timelineRect.height));
        }
    }

    /// <summary>
    /// Обрабатывает события мыши для управления нотами (перемещение, изменение размера, добавление, удаление).
    /// </summary>
    void HandleNoteEvents(Rect timelineRect)
    {
        Event e = Event.current;
        if (e == null)
            return;

        // Удаление ноты правым кликом
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            if (levelData.notes != null)
            {
                for (int i = 0; i < levelData.notes.Length; i++)
                {
                    NoteData note = levelData.notes[i];
                    float noteX = timelineRect.x + note.startTime * timeScale;
                    float laneY = timelineRect.y + note.trackIndex * trackHeight;
                    float noteWidth = Mathf.Max(note.duration * timeScale, 10f);
                    Rect noteRect = new Rect(noteX, laneY + 2, noteWidth, trackHeight - 4);
                    if (noteRect.Contains(e.mousePosition))
                    {
                        List<NoteData> noteList = new List<NoteData>(levelData.notes);
                        noteList.RemoveAt(i);
                        levelData.notes = noteList.ToArray();
                        EditorUtility.SetDirty(levelData);
                        e.Use();
                        Repaint();
                        return;
                    }
                }
            }
        }

        // Левый клик: выбор/перемещение/изменение размера или добавление ноты
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            bool clickedOnNote = false;
            if (levelData.notes != null)
            {
                for (int i = 0; i < levelData.notes.Length; i++)
                {
                    NoteData note = levelData.notes[i];
                    float noteX = timelineRect.x + note.startTime * timeScale;
                    float laneY = timelineRect.y + note.trackIndex * trackHeight;
                    float noteWidth = Mathf.Max(note.duration * timeScale, 10f);
                    Rect noteRect = new Rect(noteX, laneY + 2, noteWidth, trackHeight - 4);

                    if (noteRect.Contains(e.mousePosition))
                    {
                        clickedOnNote = true;
                        // Если курсор рядом с правым краем — начинаем изменение размера
                        float distToEdge = Mathf.Abs(e.mousePosition.x - (noteRect.x + noteRect.width));
                        if (distToEdge < resizeMargin)
                        {
                            draggingNoteIndex = i;
                            isResizing = true;
                            e.Use();
                            break;
                        }
                        else
                        {
                            // Начало перетаскивания ноты
                            draggingNoteIndex = i;
                            isResizing = false;
                            dragOffset = e.mousePosition - new Vector2(noteRect.x, noteRect.y);
                            e.Use();
                            break;
                        }
                    }
                }
            }
            // Если кликнули в пустой области, добавляем новую ноту
            if (!clickedOnNote && timelineRect.Contains(e.mousePosition))
            {
                float clickedTime = (e.mousePosition.x - timelineRect.x) / timeScale;
                int clickedTrack = Mathf.FloorToInt((e.mousePosition.y - timelineRect.y) / trackHeight);
                clickedTrack = Mathf.Clamp(clickedTrack, 0, trackCount - 1);
                AddNoteAt(clickedTime, clickedTrack);
                e.Use();
            }
        }

        // Перетаскивание и изменение размера ноты
        if (e.type == EventType.MouseDrag && e.button == 0 && draggingNoteIndex != -1)
        {
            Vector2 mousePos = e.mousePosition;
            NoteData note = levelData.notes[draggingNoteIndex];
            if (isResizing)
            {
                float noteX = timelineRect.x + note.startTime * timeScale;
                float newDuration = (mousePos.x - noteX) / timeScale;
                note.duration = Mathf.Max(newDuration, 0.1f);
                EditorUtility.SetDirty(levelData);
            }
            else
            {
                Vector2 newPos = mousePos - dragOffset;
                float newStartTime = (newPos.x - timelineRect.x) / timeScale;
                newStartTime = Mathf.Max(0, newStartTime);
                int newTrack = Mathf.FloorToInt((newPos.y - timelineRect.y) / trackHeight);
                newTrack = Mathf.Clamp(newTrack, 0, trackCount - 1);
                note.startTime = newStartTime;
                note.trackIndex = newTrack;
                EditorUtility.SetDirty(levelData);
            }
            e.Use();
            Repaint();
        }

        if (e.type == EventType.MouseUp && e.button == 0 && draggingNoteIndex != -1)
        {
            draggingNoteIndex = -1;
            isResizing = false;
            e.Use();
        }
    }

    /// <summary>
    /// Добавляет новую ноту на заданном времени и дорожке.
    /// </summary>
    void AddNoteAt(float time, int track)
    {
        if (levelData.notes == null)
            levelData.notes = new NoteData[0];

        NoteData newNote = new NoteData
        {
            startTime = time,
            duration = 0.5f,
            trackIndex = track
        };

        Array.Resize(ref levelData.notes, levelData.notes.Length + 1);
        levelData.notes[levelData.notes.Length - 1] = newNote;
        EditorUtility.SetDirty(levelData);
    }

    /// <summary>
    /// Переключает воспроизведение аудио.
    /// </summary>
    void TogglePlayback()
    {
        if (previewSource == null)
        {
            GameObject tempObject = new GameObject("PreviewAudioSource");
            previewSource = tempObject.AddComponent<AudioSource>();
            previewSource.playOnAwake = false;
            previewSource.clip = levelData.music;
        }

        isPlaying = !isPlaying;
        if (isPlaying)
        {
            previewSource.time = timelinePosition;
            previewSource.Play();
        }
        else
        {
            previewSource.Stop();
        }
    }

    void OnDestroy()
    {
        if (previewSource != null)
            DestroyImmediate(previewSource.gameObject);
    }
}
#endif
