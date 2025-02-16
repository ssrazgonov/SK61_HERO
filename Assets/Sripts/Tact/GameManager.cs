using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public LevelData levelData;
    public GameObject notePrefab;
    public Transform[] tracks;
    public float hitZoneY = -3.5f;
    public float noteSpeed = 5f;

    private List<NoteController> activeNotes = new List<NoteController>();
    private float songStartTime;
    private bool isPlaying;

    public float hitZoneZ = 10f;

    void Start()
    {
        StartLevel();
    }

    void StartLevel()
    {
        songStartTime = Time.time + levelData.startDelay;
        isPlaying = true;

        // Запуск музыки
        AudioSource.PlayClipAtPoint(
            levelData.music,
            Camera.main.transform.position,
            songStartTime - Time.time
        );

        // Запланировать все ноты
        foreach (var note in levelData.notes)
        {
            float spawnTime = note.startTime - (Mathf.Abs(hitZoneY) / noteSpeed);
            StartCoroutine(SpawnNote(note, spawnTime));
        }
    }

    System.Collections.IEnumerator SpawnNote(NoteData noteData, float spawnTime)
    {
        yield return new WaitForSeconds(spawnTime + levelData.startDelay);

        var noteObj = Instantiate(
            notePrefab,
            tracks[noteData.trackIndex].position + Vector3.up * 10f,
            Quaternion.identity
        );

        var controller = noteObj.GetComponent<NoteController>();
        controller.duration = noteData.duration;
        controller.trackIndex = noteData.trackIndex;
        controller.speed = noteSpeed;

        activeNotes.Add(controller);
    }

    void Update()
    {
        if (!isPlaying) return;

        HandleInput();
        CheckMissedNotes();
    }

void HandleInput()
{
    if (Input.GetKeyDown(KeyCode.A)) CheckNoteHit(0);
    if (Input.GetKeyDown(KeyCode.S)) CheckNoteHit(1);
    if (Input.GetKeyDown(KeyCode.D)) CheckNoteHit(2);
    if (Input.GetKeyDown(KeyCode.F)) CheckNoteHit(3);
    if (Input.GetKeyDown(KeyCode.G)) CheckNoteHit(4);
}

void CheckNoteHit(int trackIndex)
{
    foreach (var note in activeNotes.ToArray())
    {
        if (note.trackIndex != trackIndex) continue;

        // Получаем позицию и размер ноты в 3D
        float noteZ = note.transform.position.z;
        float noteLength = note.transform.localScale.z;
        float noteStart = noteZ - noteLength * 0.5f;
        float noteEnd = noteZ + noteLength * 0.5f;

        // Проверка пересечения с зоной попадания (hitZoneZ)
        if (noteEnd > hitZoneZ && noteStart < hitZoneZ)
        {
            HitNote(note);
            activeNotes.Remove(note);
            Destroy(note.gameObject);
        }
    }
}

    void HitNote(NoteController note)
    {
        // Логика начисления очков
        Debug.Log($"Hit note! Duration: {note.duration}");
    }

void CheckMissedNotes()
{
    foreach (var note in activeNotes.ToArray())
    {
        if (note.transform.position.z > hitZoneZ + 2f)
        {
            activeNotes.Remove(note);
            Destroy(note.gameObject);
            Debug.Log("Missed note!");
        }
    }
}
}