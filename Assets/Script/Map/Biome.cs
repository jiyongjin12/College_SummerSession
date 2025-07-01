using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Map/BiomeData")]
public class Biome : ScriptableObject
{
    public string biomeName; // 바이옴 이름 (예: 동굴, 산호초)
    public FishHabitat habitatType; // 이 바이옴의 FishHabitat 타입 (예: Cave, CoralReef)

    [Header("바이옴 위치 및 크기 (월드 좌표)")]
    public Vector3 center;
    public Vector3 size; 

    [Header("시각화 설정")]
    [Tooltip("기즈모에서 바이옴을 표시할 색상 문자열 (예: 'red', 'blue', '#RRGGBB', '0,1,0,0.5')")]
    public string colorString; // 기본 색상


    // 지정된 색상 문자열을 Unity Color 객체로 변환
    // 유효하지 않거나 비어있는 경우 기본값인 Red를 반환
    public Color GetGizmoColor()
    {
        if (string.IsNullOrWhiteSpace(colorString))
        {
            return Color.red; // 비어있으면 빨간색
        }

        Color parsedColor;

        // 1. HTML 색상 코드 (#RRGGBB 또는 #RRGGBBAA) 파싱 시도
        if (ColorUtility.TryParseHtmlString(colorString, out parsedColor))
        {
            return parsedColor;
        }

        // 2. 미리 정의된 색상 이름 파싱 시도 (reflection 사용)
        System.Reflection.FieldInfo colorField = typeof(Color).GetField(colorString.ToLower(), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (colorField != null)
        {
            return (Color)colorField.GetValue(null);
        }

        // 3. 콤마로 구분된 RGBA 값 파싱 시도 (예: "0.5,0.2,0.8,1.0" 또는 "128,50,200,255")
        string[] rgba = colorString.Split(',');
        if (rgba.Length >= 3 && rgba.Length <= 4)
        {
            if (float.TryParse(rgba[0].Trim(), out float r) &&
                float.TryParse(rgba[1].Trim(), out float g) &&
                float.TryParse(rgba[2].Trim(), out float b))
            {
                float a = 1f; // 기본 알파값
                if (rgba.Length == 4)
                {
                    float.TryParse(rgba[3].Trim(), out a);
                }

                // 0-255 범위 값도 지원 (나누기 255)
                if (r > 1f || g > 1f || b > 1f || a > 1f)
                {
                    r /= 255f;
                    g /= 255f;
                    b /= 255f;
                    a /= 255f;
                }

                return new Color(r, g, b, a);
            }
        }

        Debug.LogWarning($"BiomeData: '{biomeName}' 바이옴의 색상 '{colorString}'을(를) 파싱할 수 없습니다. 기본 Red 색상으로 설정합니다.");
        return Color.red; // 모든 시도가 실패하면 빨간색
    }

    // 해당 위치가 이 바이옴 내에 있는지 확인하는 메서드 (2D/3D 겸용)
    public bool Contains(Vector3 position)
    {
        Vector3 minBounds = center - size / 2f;
        Vector3 maxBounds = center + size / 2f;

        // 2D 프로젝트이므로 Z축은 생략하거나 비교 범위 내에 있는지 확인
        return position.x >= minBounds.x && position.x <= maxBounds.x &&
               position.y >= minBounds.y && position.y <= maxBounds.y &&
               position.z >= minBounds.z && position.z <= maxBounds.z; // Z축도 필요하다면 유지
    }
}
