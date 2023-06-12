using System;
using System.Collections.Generic;
using UnityEngine;

public class SE_Rested : SE_Stats
{

	public override void Setup(Character character)
	{
		base.Setup(character);
		this.UpdateTTL();
		Player player = this.m_character as Player;
		this.m_character.Message(MessageHud.MessageType.Center, "$se_rested_start ($se_rested_comfort:" + player.GetComfortLevel().ToString() + ")", 0, null);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		this.m_timeSinceComfortUpdate -= dt;
	}

	public override void ResetTime()
	{
		this.UpdateTTL();
	}

	private void UpdateTTL()
	{
		Player player = this.m_character as Player;
		float num = this.m_baseTTL + (float)(player.GetComfortLevel() - 1) * this.m_TTLPerComfortLevel;
		float num2 = this.m_ttl - this.m_time;
		if (num > num2)
		{
			this.m_ttl = num;
			this.m_time = 0f;
		}
	}

	private static int PieceComfortSort(Piece x, Piece y)
	{
		if (x.m_comfortGroup != y.m_comfortGroup)
		{
			return x.m_comfortGroup.CompareTo(y.m_comfortGroup);
		}
		float num = (float)x.GetComfort();
		float num2 = (float)y.GetComfort();
		if (num != num2)
		{
			return num2.CompareTo(num);
		}
		return y.m_name.CompareTo(x.m_name);
	}

	public static int CalculateComfortLevel(Player player)
	{
		return SE_Rested.CalculateComfortLevel(player.InShelter(), player.transform.position);
	}

	public static int CalculateComfortLevel(bool inShelter, Vector3 position)
	{
		int num = 1;
		if (inShelter)
		{
			num++;
			List<Piece> nearbyComfortPieces = SE_Rested.GetNearbyComfortPieces(position);
			nearbyComfortPieces.Sort(new Comparison<Piece>(SE_Rested.PieceComfortSort));
			int i = 0;
			while (i < nearbyComfortPieces.Count)
			{
				Piece piece = nearbyComfortPieces[i];
				if (i <= 0)
				{
					goto IL_68;
				}
				Piece piece2 = nearbyComfortPieces[i - 1];
				if ((piece.m_comfortGroup == Piece.ComfortGroup.None || piece.m_comfortGroup != piece2.m_comfortGroup) && !(piece.m_name == piece2.m_name))
				{
					goto IL_68;
				}
				IL_71:
				i++;
				continue;
				IL_68:
				num += piece.GetComfort();
				goto IL_71;
			}
		}
		return num;
	}

	private static List<Piece> GetNearbyComfortPieces(Vector3 point)
	{
		SE_Rested.s_tempPieces.Clear();
		Piece.GetAllComfortPiecesInRadius(point, 10f, SE_Rested.s_tempPieces);
		return SE_Rested.s_tempPieces;
	}

	[Header("__SE_Rested__")]
	public float m_baseTTL = 300f;

	public float m_TTLPerComfortLevel = 60f;

	private const float c_ComfortRadius = 10f;

	private float m_timeSinceComfortUpdate;

	private static readonly List<Piece> s_tempPieces = new List<Piece>();
}
