using UnityEngine;

public class WandProximity : MonoBehaviour
{
    public Transform tip;
    public Color wandColor = Color.cyan;
    public bool isGrabbed = false;
    public float spreadDistance = 2.0f;

    // Optional: detect if currently held (or just keep it always active if held)
    void Update()
    {
        Shader.SetGlobalVector("_WandPosition", tip.position);
        Shader.SetGlobalColor("_WandColor", wandColor);
        Shader.SetGlobalFloat("_WandRadius", spreadDistance); 
    }
}