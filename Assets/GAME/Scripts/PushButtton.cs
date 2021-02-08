using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PushButtton : MonoBehaviour
{
	public Texture buttonNormal;
	public Texture buttonSelected;
	public Texture icon;
	public Color color;
	public float selectTreshhold = 0.25f;
	public float chosenTreshhold = 1.5f;
	public GameObject receiver;
	public string message;
	public AudioClip tick;

	private Transform cube;
	private RawImage buttonImage, iconImage;
	private bool chosen = false;

	public void Update()
	{
		if (cube == null)
			cube = GetComponentInChildren<Rigidbody>().gameObject.transform;
		if (buttonImage == null)
			buttonImage = GetComponentsInChildren<RawImage>().FirstOrDefault(cmp => cmp.name == "bg");
		if (iconImage == null)
			iconImage = GetComponentsInChildren<RawImage>().FirstOrDefault(cmp => cmp.name == "icon");

		var push = cube.localPosition.z;
		buttonImage.texture = push > selectTreshhold ? buttonSelected : buttonNormal;
		if (chosen == false && push > chosenTreshhold)
		{
			chosen = true;
			if (receiver != null)
			{
				AudioSource.PlayClipAtPoint(tick, transform.position);
				receiver.SendMessage(message);
			}
		}
		if (push < 0.5 && chosen)
			chosen = false;

		if (iconImage != null)
		{
			iconImage.texture = icon;
			iconImage.color = color;
		}
	}

	void OnDisable()
	{
		if (cube != null)
			cube.localPosition = Vector3.zero;
	}
}
