using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Map/BiomeData")]
public class Biome : ScriptableObject
{
    public string biomeName; // ���̿� �̸� (��: ����, ��ȣ��)
    public FishHabitat habitatType; // �� ���̿��� FishHabitat Ÿ�� (��: Cave, CoralReef)

    [Header("���̿� ��ġ �� ũ�� (���� ��ǥ)")]
    public Vector3 center;
    public Vector3 size; 

    [Header("�ð�ȭ ����")]
    [Tooltip("����𿡼� ���̿��� ǥ���� ���� ���ڿ� (��: 'red', 'blue', '#RRGGBB', '0,1,0,0.5')")]
    public string colorString; // �⺻ ����


    // ������ ���� ���ڿ��� Unity Color ��ü�� ��ȯ
    // ��ȿ���� �ʰų� ����ִ� ��� �⺻���� Red�� ��ȯ
    public Color GetGizmoColor()
    {
        if (string.IsNullOrWhiteSpace(colorString))
        {
            return Color.red; // ��������� ������
        }

        Color parsedColor;

        // 1. HTML ���� �ڵ� (#RRGGBB �Ǵ� #RRGGBBAA) �Ľ� �õ�
        if (ColorUtility.TryParseHtmlString(colorString, out parsedColor))
        {
            return parsedColor;
        }

        // 2. �̸� ���ǵ� ���� �̸� �Ľ� �õ� (reflection ���)
        System.Reflection.FieldInfo colorField = typeof(Color).GetField(colorString.ToLower(), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (colorField != null)
        {
            return (Color)colorField.GetValue(null);
        }

        // 3. �޸��� ���е� RGBA �� �Ľ� �õ� (��: "0.5,0.2,0.8,1.0" �Ǵ� "128,50,200,255")
        string[] rgba = colorString.Split(',');
        if (rgba.Length >= 3 && rgba.Length <= 4)
        {
            if (float.TryParse(rgba[0].Trim(), out float r) &&
                float.TryParse(rgba[1].Trim(), out float g) &&
                float.TryParse(rgba[2].Trim(), out float b))
            {
                float a = 1f; // �⺻ ���İ�
                if (rgba.Length == 4)
                {
                    float.TryParse(rgba[3].Trim(), out a);
                }

                // 0-255 ���� ���� ���� (������ 255)
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

        Debug.LogWarning($"BiomeData: '{biomeName}' ���̿��� ���� '{colorString}'��(��) �Ľ��� �� �����ϴ�. �⺻ Red �������� �����մϴ�.");
        return Color.red; // ��� �õ��� �����ϸ� ������
    }

    // �ش� ��ġ�� �� ���̿� ���� �ִ��� Ȯ���ϴ� �޼��� (2D/3D ���)
    public bool Contains(Vector3 position)
    {
        Vector3 minBounds = center - size / 2f;
        Vector3 maxBounds = center + size / 2f;

        // 2D ������Ʈ�̹Ƿ� Z���� �����ϰų� �� ���� ���� �ִ��� Ȯ��
        return position.x >= minBounds.x && position.x <= maxBounds.x &&
               position.y >= minBounds.y && position.y <= maxBounds.y &&
               position.z >= minBounds.z && position.z <= maxBounds.z; // Z�൵ �ʿ��ϴٸ� ����
    }
}
