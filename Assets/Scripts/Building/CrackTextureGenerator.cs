using UnityEngine;

public class CrackTextureGenerator : MonoBehaviour
{
    [Header("裂缝贴图生成")]
    public int m_TextureSize = 256;
    public Color m_CrackColor = Color.black;
    public Color m_BackgroundColor = Color.clear;
    public float m_CrackWidth = 2f;
    public int m_CrackBranches = 3;
    
    [ContextMenu("生成裂缝贴图")]
    public void GenerateCrackTextures()
    {
        // 生成多个不同的裂缝贴图
        for (int i = 0; i < 5; i++)
        {
            Texture2D crackTexture = CreateCrackTexture(i);
            SaveTextureAsAsset(crackTexture, $"CrackTexture_{i}");
        }
        
        Debug.Log("裂缝贴图生成完成！");
    }
    
    private Texture2D CreateCrackTexture(int seed)
    {
        Random.InitState(seed);
        
        Texture2D texture = new Texture2D(m_TextureSize, m_TextureSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[m_TextureSize * m_TextureSize];
        
        // 初始化为透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = m_BackgroundColor;
        }
        
        // 生成主裂缝
        Vector2 startPoint = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
        Vector2 direction = Random.insideUnitCircle.normalized;
        
        DrawCrack(pixels, startPoint, direction, 1f, 0);
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return texture;
    }
    
    private void DrawCrack(Color[] pixels, Vector2 startPoint, Vector2 direction, float intensity, int depth)
    {
        if (depth > m_CrackBranches || intensity < 0.1f) return;
        
        Vector2 currentPoint = startPoint;
        float length = Random.Range(0.3f, 0.7f);
        int steps = Mathf.RoundToInt(length * m_TextureSize);
        
        for (int i = 0; i < steps; i++)
        {
            // 添加一些随机性
            Vector2 noise = Random.insideUnitCircle * 0.02f;
            currentPoint += (direction + noise) * (1f / m_TextureSize);
            
            // 确保在贴图范围内
            if (currentPoint.x < 0 || currentPoint.x > 1 || currentPoint.y < 0 || currentPoint.y > 1)
                break;
            
            // 绘制裂缝点
            DrawCrackPoint(pixels, currentPoint, intensity);
            
            // 随机生成分支
            if (Random.value < 0.1f && depth < m_CrackBranches)
            {
                Vector2 branchDirection = Quaternion.Euler(0, 0, Random.Range(-45f, 45f)) * direction;
                DrawCrack(pixels, currentPoint, branchDirection, intensity * 0.7f, depth + 1);
            }
        }
    }
    
    private void DrawCrackPoint(Color[] pixels, Vector2 point, float intensity)
    {
        int x = Mathf.RoundToInt(point.x * m_TextureSize);
        int y = Mathf.RoundToInt(point.y * m_TextureSize);
        
        int width = Mathf.RoundToInt(m_CrackWidth * intensity);
        
        for (int dx = -width; dx <= width; dx++)
        {
            for (int dy = -width; dy <= width; dy++)
            {
                int px = x + dx;
                int py = y + dy;
                
                if (px >= 0 && px < m_TextureSize && py >= 0 && py < m_TextureSize)
                {
                    int index = py * m_TextureSize + px;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (distance <= width)
                    {
                        float alpha = 1f - (distance / width);
                        Color crackColor = m_CrackColor;
                        crackColor.a = alpha * intensity;
                        
                        // 混合颜色
                        pixels[index] = Color.Lerp(pixels[index], crackColor, alpha);
                    }
                }
            }
        }
    }
    
    private void SaveTextureAsAsset(Texture2D texture, string fileName)
    {
        #if UNITY_EDITOR
        byte[] bytes = texture.EncodeToPNG();
        string path = $"Assets/Textures/{fileName}.png";
        
        // 确保目录存在
        System.IO.Directory.CreateDirectory("Assets/Textures");
        
        System.IO.File.WriteAllBytes(path, bytes);
        UnityEditor.AssetDatabase.Refresh();
        
        // 设置贴图导入设置
        UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
        if (importer != null)
        {
            importer.textureType = UnityEditor.TextureImporterType.Default;
            importer.alphaSource = UnityEditor.TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        #endif
    }
}