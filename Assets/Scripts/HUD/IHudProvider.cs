using UnityEngine;

/// <summary>
/// Fournit les données nécessaires à l'affichage du HUD avion.
/// Toutes les unités sont en métrique sauf indication contraire.
/// </summary>
public interface IHudProvider
{
    /// <summary>Vitesse air en m/s.</summary>
    float SpeedMs { get; }
    /// <summary>Vitesse air en noeuds.</summary>
    float SpeedKnots { get; }
    /// <summary>Altitude (m) relative à l'origine monde (Y).</summary>
    float AltitudeMeters { get; }
    /// <summary>Cap magnétique/simplifié (deg 0-360) basé sur l'axe Y du transform.</summary>
    float HeadingDegrees { get; }
    /// <summary>Angle de tangage (pitch) en degrés (+ nez vers le haut).</summary>
    float PitchDegrees { get; }
    /// <summary>Angle de roulis (roll) en degrés (+ aile droite vers le bas).</summary>
    float RollDegrees { get; }
    /// <summary>Vitesse verticale (m/s).</summary>
    float VerticalSpeed { get; }
    /// <summary>Latitude approximative (si mapping géographique activé).</summary>
    double Latitude { get; }
    /// <summary>Longitude approximative (si mapping géographique activé).</summary>
    double Longitude { get; }
    /// <summary>Position monde brute.</summary>
    Vector3 WorldPosition { get; }
}
