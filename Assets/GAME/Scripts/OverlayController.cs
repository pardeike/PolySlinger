using UnityEngine;
using UnityEngine.UI;

public class OverlayController : MonoBehaviour
{
	public Texture[] hearts;
	public RawImage[] health;
	public Text score;
	public Text level;
	public PieceTimer timer;

	Occupation trackedPiece;

	void Start()
	{
		var rectTransform = gameObject.GetComponent<RectTransform>();
		var f = Settings.Instance.overlayScale;
		rectTransform.sizeDelta = new Vector2(450 * f, 240 * f);
		gameObject.SetActive(f >= 0.75f);
	}

	public void UpdateLifes(int lifes)
	{
		if (lifes < 0) lifes = 0;
		if (lifes > 5) lifes = 5;
		for (var i = 0; i < 5; i++)
			health[i].texture = hearts[i < lifes ? 1 : 0];
	}

	public void UpdateScore(int value)
	{
		score.text = value.ToString();
	}

	public void UpdateLevel(int value)
	{
		level.text = value.ToString();
	}

	public void SetTrackedPiece(Occupation piece)
	{
		trackedPiece = piece;
	}

	void Update()
	{
		if (Tower.Instance.gameEnded) return;

		if (trackedPiece == null)
			timer.gameObject.SetActive(false);
		else
		{
			var timeInfo = trackedPiece.TimeLeft;
			if (timeInfo.Item1 < 0)
				timer.gameObject.SetActive(false);
			else
			{
				timer.gameObject.SetActive(true);
				timer.UpdateTime(timeInfo);
			}
		}
	}
}
