using UnityEngine;

public class Note : MonoBehaviour
{
    // Скорость движения ноты вниз
    public float speed = 5f;
    // Индекс полосы, в которой находится нота
    public int laneIndex;
    // Эффект частиц, который будет воспроизводиться при попадании по ноте
    public ParticleSystem hitEffect;
    // Плавное вращение ноты для красивого визуального эффекта
    public float rotationSpeed = 100f;

    public float startTime; // Время начала ноты в секундах
    public float duration; // Длительность ноты в секундах

    public bool isHit = false;

    void Update()
    {
        // Перемещаем ноту вниз с заданной скоростью
        transform.Translate(-Vector3.forward * speed * Time.deltaTime, Space.World);

        // Если нота уходит за пределы экрана, уничтожаем её
        if (transform.position.z < -6f)
        {
            Destroy(gameObject);
        }
    }

    // Метод, вызываемый при попадании по ноте
    public void Hit()
    {
        // Если задан эффект, создаём его в позиции ноты
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        // Уничтожаем ноту
        Destroy(gameObject);
    }

    public void Miss()
    {

    }
}
