// LevelData.cs
using System;
using UnityEngine;

[Serializable]
public class NoteData
{
    public float startTime; // Время начала ноты в секундах
    public float duration; // Длительность ноты в секундах
    public int trackIndex; // Номер дорожки (0-4)
}

[CreateAssetMenu(fileName = "NewLevel", menuName = "Rhythm Level")]
public class LevelData : ScriptableObject
{
    public AudioClip music;
    public float bpm = 120;
    public float startDelay = 2f;
    public NoteData[] notes;
}