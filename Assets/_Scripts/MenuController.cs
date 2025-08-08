// MenuController.cs
using System.Collections.Generic;
using UnityEngine;

public class MenuController : MonoBehaviour
{
	[Tooltip("Все CanvasGroup’ы, которые нужно скрыть/показать вместе с меню (кнопки, панели и т.п.)")]
	public List<CanvasGroup> menuTargets;
	[Tooltip("Фон меню")]
	public CanvasGroup menuBG;
	[Tooltip("Полноэкранный оверлей для плавного затемнения/осветления")]
	public CanvasGroup fadeOverlay;

	/// <summary>Фейдит все menuTargets и menuBG к targetAlpha за duration.</summary>
	public void FadeAll(float targetAlpha, float duration)
	{
		foreach (var cg in menuTargets)
		{
			LeanTween.cancel(cg.gameObject);
			LeanTween.alphaCanvas(cg, targetAlpha, duration)
				.setEase(LeanTweenType.easeInOutQuad);
		}

		if (menuBG != null)
		{
			LeanTween.cancel(menuBG.gameObject);
			LeanTween.alphaCanvas(menuBG, targetAlpha, duration)
				.setEase(LeanTweenType.easeInOutQuad);
		}
	}

	public void FadeInAll(float duration) => FadeAll(1f, duration);
	public void FadeOutAll(float duration) => FadeAll(0f, duration);

	/// <summary>Фейдит только оверлей к targetAlpha за duration.</summary>
	public void FadeOverlay(float targetAlpha, float duration)
	{
		if (fadeOverlay == null) return;
		LeanTween.cancel(fadeOverlay.gameObject);
		LeanTween.alphaCanvas(fadeOverlay, targetAlpha, duration)
			.setEase(LeanTweenType.easeInOutQuad);
	}

	/// <summary>Мгновенно скрывает все menuTargets и menuBG.</summary>
	public void HideMenuInstant()
	{
		foreach (var cg in menuTargets)
			cg.alpha = 0f;

		if (menuBG != null)
			menuBG.alpha = 0f;
	}
}