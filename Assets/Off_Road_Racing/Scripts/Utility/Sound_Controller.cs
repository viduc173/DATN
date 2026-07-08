using ALIyerEdon;
using UnityEngine;
using UnityEngine.UI;
namespace ALIyerEdon
{
    public class Sound_Controller : MonoBehaviour
    {

        public Slider engine, collision, flame, music, nitro,horns;
        public Text engineInfo, collisionInfo, flameInfo, musicInfo, nitroInfo, hornsInfo;
        public AudioSource[] hornsSources;

        public void Update_Sounds()
        {

            engineInfo.text = engine.value.ToString();
            collisionInfo.text = collision.value.ToString();
            flameInfo.text = flame.value.ToString();
            nitroInfo.text = nitro.value.ToString();
            musicInfo.text = music.value.ToString();
            hornsInfo.text = horns.value.ToString();

            foreach (EasyCarAudio carAudio in FindObjectsOfType<EasyCarAudio>())
            {
                carAudio.engineVolume = engine.value;
                carAudio.collisionVolume = collision.value;
                carAudio.collisionSource.volume = collision.value;
                carAudio.flameSource.volume = flame.value;
            }

            FindFirstObjectByType<Load_Settings>().music.volume = music.value;

            FindFirstObjectByType<Nitro_Feature>().nitroSource.volume = nitro.value;

            foreach (AudioSource horn in hornsSources)
                horn.volume = horns.value;

        }
    }
}