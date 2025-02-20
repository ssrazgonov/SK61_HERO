// LevelData.cs
using System;
using UnityEngine;

[Serializable]
public class NoteData
{
    public float startTime; // Время начала ноты в секундах
    public float duration; // Длительность ноты в секундах
    public int trackIndex; // Номер дорожки (0-4)

    public bool spawned = false;
}

[CreateAssetMenu(fileName = "NewLevel", menuName = "Rhythm Level")]
public class LevelData : ScriptableObject
{
    public AudioClip music;
    public float bpm = 120;
    public float startDelay = 2f;
    public NoteData[] notes;

    [Header("Rhythm Settings")]
    public int beatsPerBar = 4; // Долей в такте
    public int subdivisions = 4;

    public float audioOffset = 0;
}