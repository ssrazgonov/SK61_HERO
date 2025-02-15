// Класс, описывающий один бит
[System.Serializable]
public class Beat
{
    // Время (в секундах) появления ноты
    public float time;
    // Индекс полосы, в которой появится нота
    public int lane;
}