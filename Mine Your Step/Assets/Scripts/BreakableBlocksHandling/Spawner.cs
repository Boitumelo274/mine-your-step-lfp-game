using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] GameObject _object;

    public void Spawn()
    {
        Instantiate(_object, transform.position,Quaternion.identity);
    }

}
