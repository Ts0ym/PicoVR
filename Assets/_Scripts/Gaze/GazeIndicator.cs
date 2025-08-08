using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(Image))]
public class GazeIndicator : MonoBehaviour
{
	[Tooltip("Скорость заполнения/опустошения (в fillAmount/sec)")]
	public float fillSpeed = 1f;

	/// <summary>Событие, когда fillAmount достиг 1</summary>
	public UnityEvent onFilled;

	[HideInInspector] public float fillAmount;  // текущее значение
	private Image img;
	private bool isGazed;
	private bool hasFired;

	void Awake()
	{
		img = GetComponent<Image>();
		img.type = Image.Type.Filled;
		img.fillMethod = Image.FillMethod.Radial360;
		img.fillAmount = 0f;
		fillAmount = 0f;
		hasFired = false;
	}

	void Update()
	{
		float target = isGazed ? 1f : 0f;
		fillAmount = Mathf.MoveTowards(fillAmount, target, fillSpeed * Time.deltaTime);
		img.fillAmount = fillAmount;

		if (fillAmount >= 1f && !hasFired)
		{
			hasFired = true;
			onFilled?.Invoke();
		}
		else if (fillAmount <= 0f)
		{
			hasFired = false;
		}
	}

	/// <summary>Внешний вызов: начать/продолжить заполнение</summary>
	public void SetGazeState(bool gazed)
	{
		isGazed = gazed;
	}

	/// <summary>Принудительно обнулить индикатор</summary>
	public void ForceReset()
	{
		isGazed = false;
		fillAmount = 0f;
		img.fillAmount = 0f;
		hasFired = false;
	}
}