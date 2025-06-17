
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    static T instance;
    public static T Instance
    {
        get
        {
            
            //先找一下 是否已经有这个类  因为有的单例是不用挂载的，用到的时候让下面的那个判断 自动生成一个就行
            //但有的单例可能已经挂载在游戏物体上了  如果不先走这步的话  那就会再创建一个实例 而且返回的是新建的那个
            if (instance==null)
            {
               instance=FindObjectOfType<T>();             
            }
            if (instance == null)
            {
                var go = new GameObject(typeof(T).Name);
                DontDestroyOnLoad(go);
                instance = go.AddComponent<T>();
            }
            return instance;
        }
    }
}