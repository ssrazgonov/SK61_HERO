using UnityEngine;

public class NoteController : MonoBehaviour
{
    public float duration;
    public int trackIndex;
    public float speed = 5f;
    
    private MeshRenderer meshRenderer;
    private BoxCollider collider;
    private Vector3 startScale;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        collider = GetComponent<BoxCollider>();
        startScale = transform.localScale;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        // Растягиваем ноту вдоль оси движения (Z)
        float length = duration * speed;
        transform.localScale = new Vector3(
            startScale.x,
            startScale.y,
            length
        );
        
        // Настраиваем коллайдер
        collider.size = new Vector3(
            0.8f, 
            0.8f, 
            length
        );
    }

    void Update()
    {
        // Движение вперед по оси Z
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}