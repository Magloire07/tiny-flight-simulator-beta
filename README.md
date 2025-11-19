# tiny-flight-simulator-beta
## HUD (Vitesse, Altitude, Cap, Horizon, GPS)

Scripts ajoutés dans `Assets/Scripts/HUD` :
- `IHudProvider` : interface des données.
- `PlaneHudProvider` : collecte les infos depuis `Plane` + `Rigidbody`.
- `PlaneHUDUI` : met à jour les éléments UI et l'horizon artificiel.
	- Supporte jauge `Throttle` (Image Filled), jauge `Fuel` (Image Filled), barre VSI (Image Filled verticale).

### Intégration Rapide
1. Sélectionner l'avion dans la scène, ajouter le composant `PlaneHudProvider` (ou présent automatiquement si placé comme enfant).
2. Créer un `Canvas` (Screen Space - Overlay). Ajouter des `Text` (Legacy UI) pour : Speed, Altitude, Heading, Pitch, Roll, Vertical Speed, GPS.
3. Créer deux `Image`/`RectTransform` pour l'horizon artificiel :
	- Parent (roll layer) : pivot centré, ce composant pivote sur Z.
	- Enfant (pitch layer) : se translate verticalement selon le pitch.
4. Ajouter le script `PlaneHUDUI` sur le Canvas. Renseigner les références Text + les deux `RectTransform` horizon + le `PlaneHudProvider`.
5. Pour les jauges :
	- Créer deux `Image` type `Filled` (Throttle et Fuel). `Fill Method` = `Radial 360` (ou 180 selon votre visuel). Assigner aux champs `throttleArc` et `fuelArc`. Lier leurs `Text` respectifs si souhaité.
	- Pour la `VSI`, créer une `Image` type `Filled` (Vertical) et l'assigner à `vsiBar`. Lier `vsiText` pour afficher la valeur.
6. (Optionnel) Activer le mapping géographique (latitude/longitude) sur `PlaneHudProvider` si vous souhaitez des coordonnées approximatives.

### Réglages Principaux
- `pitchPixelsPerDegree` : sensibilité de la translation verticale de l'horizon.
- `smoothFactor` : lissage visuel (0 = instantané, 1 = pas de mise à jour).
- Conversion vitesse en noeuds : interne (m/s * 1.943844).
 - `fuel01` : valeur 0..1 si vous ne disposez pas encore d'un système carburant.
 - `vsiRange` : plage m/s utilisée pour mapper la barre (par défaut ±20 m/s).

### Extension / Personnalisation
Vous pouvez remplacer les `Text` par TextMeshPro : créer un script dérivé ou simplement changer le type des champs si TMP est installé.
Pour afficher une altitude différente (ex: par rapport au terrain), injecter un autre provider implémentant `IHudProvider`.

### Exemple Format Affiché
```
SPD 120 kt
ALT 1500 m
HDG 045°
PITCH +5.2° ROLL -10.3°
VS +3.5 m/s
LAT 48.86012
LON 2.34567
```

### Idées Futures
- Ajout indicateur de décrochage (stall) depuis `Plane.IsStalled`.
- Barre de throttle / RPM.
- Indicateur G, taux de virage, vecteur vitesse.
- Mode nuit (palette couleurs différente).