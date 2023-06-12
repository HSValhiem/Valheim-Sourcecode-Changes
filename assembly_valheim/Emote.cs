using System;

public class Emote : Attribute
{

	public static void DoEmote(Emotes emote)
	{
		Emote attributeOfType = emote.GetAttributeOfType<Emote>();
		if (Player.m_localPlayer && Player.m_localPlayer.StartEmote(emote.ToString().ToLower(), attributeOfType == null || attributeOfType.OneShot) && attributeOfType != null && attributeOfType.FaceLookDirection)
		{
			Player.m_localPlayer.FaceLookDirection();
		}
	}

	public bool OneShot = true;

	public bool FaceLookDirection;
}
