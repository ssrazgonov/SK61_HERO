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

        // Рассчитываем размеры таймлайна
        float timelineWidth = (levelData.music != null) ? levelData.music.length * timeScale : position.width - 20;
        float timelineHeight = trackCount * trackHeight;

        GUILayout.Label("Timeline (левый клик: добавление/перемещение/изменение размера, правый клик: удаление)");

        // Оборачиваем область таймлайна в горизонтальный scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(timelineHeight + 20));
        Rect timelineRect = GUILayoutUtility.GetRect(timelineWidth, timelineHeight);

        DrawTimelineBackground(timelineRect);
        DrawNotes(timelineRect);
        HandleNoteEvents(timelineRect);

        EditorGUILayout.EndScrollView();

        // Обновление позиции воспроизведения, если музыка играет
        if (previewSource != null && previewSource.isPlaying)
        {
            timelinePosition = previewSource.time;
            Repaint();
        }

            // Поля для настроек ритма
        if (levelData != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rhythm Settings", EditorStyles.boldLabel);
            levelData.bpm = EditorGUILayout.FloatField("BPM", levelData.bpm);
            levelData.beatsPerBar = EditorGUILayout.IntField("Beats Per Bar", levelData.beatsPerBar);
            levelData.subdivisions = EditorGUILayout.IntField("Subdivisions", levelData.subdivisions);
        }
    }

    void DrawTimelineBackground(Rect timelineRect)
    {
        // Фон таймлайна
        EditorGUI.DrawRect(timelineRect, new Color(0.2f, 0.2f, 0.2f));

        // Рисуем дорожки с чередующимися оттенками
        for (int i = 0; i < trackCount; i++)
        {
            Rect laneRect = new Rect(timelineRect.x, timelineRect.y + i * trackHeight, timelineRect.width, trackHeight);
            Color laneColor = (i % 2 == 0) ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.35f, 0.35f, 0.35f);
            EditorGUI.DrawRect(laneRect, laneColor);

            // Рисуем границы дорожки
            Handles.color = Color.black;
            Handles.DrawLine(new Vector3(laneRect.x, laneRect.y), new Vector3(laneRect.x + laneRect.width, laneRect.y));
            Handles.DrawLine(new Vector3(laneRect.x, laneRect.y + laneRect.height), new Vector3(laneRect.x + laneRect.width, laneRect.y + laneRect.height));
        }

            // Рисуем ритмические метки только если есть данные о ритме
    if (levelData.bpm > 0 && levelData.beatsPerBar > 0)
    {
        float secondsPerBeat = 60f / levelData.bpm;
        float secondsPerSubdivision = secondsPerBeat / levelData.subdivisions;
        float totalTime = levelData.music != null ? levelData.music.length : 60f;

        // Рассчитываем общее количество подразделений
        int totalSubdivisions = Mathf.CeilToInt(totalTime / secondsPerSubdivision);

        for (int i = 0; i < totalSubdivisions; i++)
        {
            float time = i * secondsPerSubdivision;
            float xPos = timelineRect.x + time * timeScale;
            
            // Определяем тип линии
            bool isBar = (i % (levelData.beatsPerBar * levelData.subdivisions)) == 0;
            bool isBeat = (i % levelData.subdivisions) == 0;
            
            Color lineColor = isBar ? Color.yellow : 
                            isBeat ? Color.red : 
                            new Color(0.5f, 0.5f, 0.5f, 0.3f);
            float lineHeight = isBar ? trackHeight : 
                             isBeat ? trackHeight * 0.75f : 
                             trackHeight * 0.5f;
            float lineWidth = isBar ? 2f : 1f;

            // Рисуем линию через Handles
            Handles.color = lineColor;
            Vector2 start = new Vector2(xPos, timelineRect.y);
            Vector2 end = new Vector2(xPos, timelineRect.y + lineHeight);
            Handles.DrawLine(start, end, lineWidth);
        }
    }
    }

    void DrawNotes(Rect timelineRect)
    {
        if (levelData.notes != null)
        {
            for (int i = 0; i < levelData.notes.Length; i++)
            {
                NoteData note = levelData.notes[i];
                // Пропускаем ноты, принадлежащие несуществующим дорожкам
                if (note.trackIndex < 0 || note.trackIndex >= trackCount)
                    continue;

                float noteX = timelineRect.x + note.startTime * timeScale;
                float laneY = timelineRect.y + note.trackIndex * trackHeight;
                float noteWidth = Mathf.Max(note.duration * timeScale, 10f);
                Rect noteRect = new Rect(noteX, laneY + 2, noteWidth, trackHeight - 4);

                EditorGUI.DrawRect(noteRect, Color.green);

                // Рисуем рамку ноты
                Handles.color = Color.black;
                Handles.DrawAAPolyLine(2f, new Vector3(noteRect.x, noteRect.y), new Vector3(noteRect.x + noteRect.width, noteRect.y));
                Handles.DrawAAPolyLine(2f, new Vector3(noteRect.x, noteRect.y + noteRect.height), new Vector3(noteRect.x + noteRect.width, noteRect.y + noteRect.height));
                Handles.DrawAAPolyLine(2f, new Vector3(noteRect.x, noteRect.y), new Vector3(noteRect.x, noteRect.y + noteRect.height));
                Handles.DrawAAPolyLine(2f, new Vector3(noteRect.x + noteRect.width, noteRect.y), new Vector3(noteRect.x + noteRect.width, noteRect.y + noteRect.height));
            }
        }
    }

    void HandleNoteEvents(Rect timelineRect)
    {
        Event e = Event.current;
        if (e == null)
            return;

        // Удаление ноты при правом клике
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

        // Левый клик: добавление, перемещение, изменение размера
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
                        // Если курсор рядом с правым краем – начинаем изменение длительности
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
                            // Начинаем перетаскивание ноты
                            draggingNoteIndex = i;
                            isResizing = false;
                            dragOffset = e.mousePosition - new Vector2(noteRect.x, noteRect.y);
                            e.Use();
                            break;
                        }
                    }
                }
            }
            // Если клик в пустой области таймлайна – добавляем новую ноту
            if (!clickedOnNote && timelineRect.Contains(e.mousePosition))
            {
                float clickedTime = (e.mousePosition.x - timelineRect.x) / timeScale;
                int clickedTrack = Mathf.FloorToInt((e.mousePosition.y - timelineRect.y) / trackHeight);
                clickedTrack = Mathf.Clamp(clickedTrack, 0, trackCount - 1);
                AddNoteAt(clickedTime, clickedTrack);
                e.Use();
            }
        }

        // Перетаскивание и изменение размера при зажатой левой кнопке
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
