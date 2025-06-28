using UnityEngine;

// Class which fades away the ghost hand when it is close to the physical hand
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class ProximityHandFade : MonoBehaviour {
	private SkinnedMeshRenderer _renderer;
	private Material _mat;
	private float _originalAlpha;
	protected void Awake() {
		_renderer = GetComponent<SkinnedMeshRenderer>();
		_mat = new Material(_renderer.material);
		_renderer.material = _mat;
		_originalAlpha = _mat.color.a;
	}

	// The bones to check the distance of
	public Transform physicsBone, ghostBone;
	// When the bones are further than this distance away, make the hand "completely" opaque
	public float distanceThreshold = .1f;

	public void Update() {
		var color = _mat.color;
		color.a = Mathf.Min((physicsBone.position - ghostBone.position).magnitude / distanceThreshold, 1) * _originalAlpha;
		_mat.color = color;
	}
}