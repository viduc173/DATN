//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ALIyerEdon;

namespace ALIyerEdon
{
    public class Race_Manager : MonoBehaviour
    {
        private class Racer_Position
        {
            public int ID;
            public string Name;
            public float Position;
        }

        [Header("Options ____________________________________________________" +
            "____________________________________________________")]
        [Space(5)]
        public int levelID = 0;
        public string trackName = "Level 1";

        public bool startCutscene = false;

        [HideInInspector] public bool showLocalPosition = false;

        [Header("Race Start ____________________________________________________" +
            "____________________________________________________")]
        [Space(5)]
        public float timeScale = 1f;
        int counterNumbers = 3;
        public int totalLaps = 3;
        [HideInInspector] public GameObject startCounter;

        [Header("Minimap Icons ____________________________________________________" +
    "____________________________________________________")]
        public GameObject playerArrow;
        public GameObject racerArrow;
        public float yOffset = 10f;
        public float scale = 10f;

        [Header("User Interface ____________________________________________________" +
            "____________________________________________________")]
        [Space(5)]
        public TrackCamera_Manager trackCamera;
        public GameObject startUI;
        public GameObject raceUI;
        public GameObject raceFinishUI;
        public GameObject positionUI;
        public GameObject mobileControls;

        [Header("Player Info ____________________________________________________" +
            "____________________________________________________")]
        [Space(5)]
        public UnityEngine.UI.Text playerInfo;
        public UnityEngine.UI.Text lapInfo;
        public UnityEngine.UI.Text[] racerInfo;


        // Racers info class    
        List<Racer_Position> positions = new List<Racer_Position>();
        List<Racer_Position> sortedPositions = new List<Racer_Position>();

        [Header("Racing Elements ____________________________________________________" +
            "____________________________________________________")]
        [Space(5)]
        // Name of the each racer in order
        [HideInInspector] public string[] racerNames;

        // Player cars to spawn at the spawn points
        public GameObject[] playerPrefabs;
        GameObject playerPrefab;

        // Racer cars to spawn at the spawn points
        public GameObject[] racerPrefabs;

        [HideInInspector] public GameObject[] totalRacerPrefabs;

        // Spawn point for each racer in order
        public Transform[] spawnPositions;

        Car_Position[] carPositions;

        Car_Position playerPosition;

        [HideInInspector] public bool raceStarted;

        bool dontGetKey = false;
        string playerName = "Player";
        bool canStart;

        void Start()
        {
#if UNITY_EDITOR
            Cursor.visible = true;
#else
            Cursor.visible = false;
#endif

            AudioListener.volume = 1f;
            Time.timeScale = timeScale;

            if (PlayerPrefs.GetInt("Target FPS") > 25)
                Application.targetFrameRate = PlayerPrefs.GetInt("Target FPS");

            if (startUI) startUI.SetActive(false);
            if (raceUI) raceUI.SetActive(false);
            if (mobileControls) mobileControls.SetActive(false);

            // Build racer array — index 0 là placeholder cho player
            totalRacerPrefabs = new GameObject[racerPrefabs.Length + 1];
            totalRacerPrefabs[0] = null;
            for (int i = 1; i < totalRacerPrefabs.Length; i++)
                totalRacerPrefabs[i] = racerPrefabs[i - 1];

            carPositions = new Car_Position[totalRacerPrefabs.Length];
            racerNames = new string[totalRacerPrefabs.Length];

            GameObject playerCar = GameObject.FindGameObjectWithTag("Player");
            var posTracker = FindFirstObjectByType<RacePositionTracker>();

            for (int i = 0; i < totalRacerPrefabs.Length; i++)
            {
                GameObject racer;

                if (i == 0)
                {
                    // Xe player đã có sẵn trong scene
                    racer = playerCar;

                    if (racer != null && playerArrow != null)
                    {
                        var arrow = Instantiate(playerArrow,
                            racer.transform.position + Vector3.up * yOffset,
                            Quaternion.identity);
                        arrow.transform.SetParent(racer.transform);
                        arrow.transform.localScale = Vector3.one * scale;
                        arrow.transform.localRotation = new Quaternion(1f, 0, 0, 1f);
                        arrow.name = "Player Minimap Arrow";
                    }
                }
                else
                {
                    // Spawn AI racer ngay lập tức
                    racer = Instantiate(totalRacerPrefabs[i],
                        spawnPositions[i].position, spawnPositions[i].rotation);

                    if (racerArrow != null)
                    {
                        var arrow = Instantiate(racerArrow,
                            racer.transform.position + Vector3.up * yOffset,
                            Quaternion.identity);
                        arrow.transform.SetParent(racer.transform);
                        arrow.transform.localScale = Vector3.one * scale;
                        arrow.transform.localRotation = new Quaternion(1f, 0, 0, 1f);
                        arrow.name = "Racer Minimap Arrow";
                    }

                    // AI đứng yên cho đến khi MatchWaitTime báo bắt đầu
                    var ai = racer.GetComponent<Car_AI>();
                    if (ai != null) ai.raceStarted = false;

                    var ctrl = racer.GetComponent<EasyCarController>();
                    if (ctrl != null) { ctrl.handBrake = true; ctrl.Clutch = true; }

                    // Đăng ký với RacePositionTracker để nó track xếp hạng
                    posTracker?.RegisterAIRacer(racer.transform, $"AI {i}");
                }

                if (racer == null) continue;

                var carPos = racer.GetComponent<Car_Position>();
                if (carPos != null)
                {
                    carPos.displayPosition = false;
                    carPos.RacerID = i;
                    carPositions[i] = carPos;
                    racerNames[i] = carPos.RacerName;
                    var rp = new Racer_Position { Name = carPos.RacerName, Position = 0 };
                    positions.Add(rp);
                    sortedPositions.Add(rp);
                }
            }

            // Tham chiếu xe player để Race_Manager track xếp hạng nội bộ (tùy chọn)
            playerPosition = playerCar?.GetComponent<Car_Position>();
            playerName = playerPosition?.RacerName ?? "Player";
            startCounter = FindFirstObjectByType<Start_Counter>()?.gameObject;

            // Subscribe MatchWaitTime — khi countdown kết thúc mới cho AI chạy
            var matchWait = FindFirstObjectByType<MatchWaitTime>();
            if (matchWait != null)
                matchWait.onRaceStarted.AddListener(OnRaceStarted);
            else
                Debug.LogWarning("[Race_Manager] Không tìm thấy MatchWaitTime — AI sẽ không tự khởi động.");
        }

        /// <summary>
        /// Gọi bởi MatchWaitTime.onRaceStarted khi countdown kết thúc.
        /// </summary>
        void OnRaceStarted()
        {
            raceStarted = true;

            foreach (var ai in FindObjectsOfType<Car_AI>())
            {
                ai.raceStarted = true;
                var ctrl = ai.GetComponent<EasyCarController>();
                if (ctrl != null) { ctrl.handBrake = false; ctrl.Clutch = false; }
            }

            foreach (var rn in FindObjectsOfType<Racer_Nitro>())
                rn.raceIsStarted = true;

            // Cho phép AI kiểm tra reverse sau 2 giây
            StartCoroutine(EnableAIReverseCheck());
        }

        IEnumerator EnableAIReverseCheck()
        {
            yield return new WaitForSeconds(2f);
            foreach (var ai in FindObjectsOfType<Car_AI>())
                if (ai != null) ai.canReverseCheck = true;
        }
        public void Show_StartUI()
        {
            StartCoroutine(StartUI_Delay());
        }
        IEnumerator StartUI_Delay()
        {
            yield return new WaitForSeconds(0.001f);

            startUI.SetActive(true);
            canStart = true;

            Update_Positions_Display();

            FindFirstObjectByType<InputSystem>().canStartRace = true;

        }
        public void Update_Positions_Display()
        {
            for (int a = 0; a < FindFirstObjectByType<Start_Finish_UI>().positions.Length; a++)
            {
                    FindFirstObjectByType<Start_Finish_UI>().driversName[a].text =
                       sortedPositions[a].Name.ToString();
                
            }

            startUI.GetComponent<Start_Finish_UI>().totalScores.text =
                "Total Coins : " +
                PlayerPrefs.GetInt("TotalScores").ToString();
        }
        public void StartRace_Button()
        {
            if (!dontGetKey)
            {
                foreach (EasyCarAudio carAudio in FindObjectsOfType<EasyCarAudio>())
                {
                    carAudio.engineVolume = carAudio.engineStartVolume;
                    carAudio.engineSource.volume = carAudio.engineStartVolume;
                }

                FindFirstObjectByType<InputSystem>().raceIsStarted = true;

                StartRace();
                dontGetKey = true;
            }
        }
        public void StartRace()
        {
            if (FindFirstObjectByType<FadeMode>())
                FindFirstObjectByType<FadeMode>().Do_Fade();
            
            StartCoroutine(StartRaceDelay());
        }
        IEnumerator StartRaceDelay()
        {
            GameObject.FindGameObjectWithTag("Player").GetComponent
            <EasyCarAudio>().engineSource.volume = GameObject.FindGameObjectWithTag("Player").GetComponent
            <EasyCarAudio>().engineStartVolume;

            if (FindFirstObjectByType<Start_Cutscene>())
                FindFirstObjectByType<Start_Cutscene>().Start_Race();

            FindFirstObjectByType<InputSystem>().canControl = true;

            if (startUI)
                startUI.SetActive(false);
            if (raceUI)
                raceUI.SetActive(true);

            if (GetComponentInChildren<InputSystem>().controlMode == Control_Type.Touch)
                mobileControls.SetActive(true);
            else
                mobileControls.SetActive(false);

            Update_SideUI();
            
            yield return new WaitForSeconds(1);

            FindFirstObjectByType<Start_Counter>().StartCounter();

            yield return new WaitForSeconds((counterNumbers) * timeScale);

            foreach (Car_AI carAI in FindObjectsOfType<Car_AI>())
            {
                carAI.raceStarted = true;
                carAI.gameObject.GetComponent<EasyCarController>()
                    .handBrake = false;
                carAI.gameObject.GetComponent<EasyCarController>()
                    .Clutch = false;
            }

            GameObject.FindGameObjectWithTag("Player")
                .GetComponent<EasyCarController>().Clutch = false;

            GameObject.FindGameObjectWithTag("Player")
                .GetComponent<EasyCarController>().handBrake = false;
            
            GameObject.FindGameObjectWithTag("Player")
                                .GetComponent<EasyCarAudio>().stopRandom = true;

            if (GameObject.FindGameObjectWithTag("Player")
                .GetComponent<EasyCarController>().throttleInput > 0.6f)
            {
                GameObject.FindGameObjectWithTag("Player")
                                .GetComponent<EasyCarAudio>().Play_StartSkid_Sound();

            }

            foreach (GameObject racerCars in GameObject.FindGameObjectsWithTag("Racer"))
                racerCars.GetComponent<EasyCarAudio>().Play_StartSkid_Sound();

            // User can display the pause menu after race start
            FindFirstObjectByType<Pause_Menu>().raceIsStarted = true;
            FindFirstObjectByType<Nitro_Feature>().raceIsStarted = true;

            foreach (Racer_Nitro rn in GameObject.FindObjectsOfType<Racer_Nitro>())
                rn.raceIsStarted = true;

            yield return new WaitForSeconds(
                GameObject.FindGameObjectWithTag("Player")
                .GetComponent<EasyCarController>().startDuration);

            GameObject.FindGameObjectWithTag("Player")
                .GetComponent<EasyCarController>().shaking = false;
            yield return new WaitForSeconds(1f);

            // Racers can check reverse mode after 2 seconds from the race start 
            foreach (Car_AI carAI in FindObjectsOfType<Car_AI>())
                carAI.canReverseCheck = true;

        }
        public void Finish_Race()
        {
            StartCoroutine(Race_Sinish_Manager());
        }

        IEnumerator Race_Sinish_Manager()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                var playerAI = playerObj.GetComponent<Car_AI>();
                if (playerAI != null) playerAI.enabled = true;
                var ctrl = playerObj.GetComponent<EasyCarController>();
                if (ctrl != null) ctrl.brakePower = 1023f;
                var camSwitch = playerObj.GetComponent<CameraSwitch>();
                if (camSwitch != null) camSwitch.SelectCamera(3);
            }

            var inputSys = FindFirstObjectByType<InputSystem>();
            if (inputSys != null) { inputSys.canControl = false; inputSys.finishedRace = true; }
            mobileControls.SetActive(false);

            trackCamera.enabled = true;

            startUI.GetComponent<Start_Finish_UI>().startButton.SetActive(false);
            startUI.GetComponent<Start_Finish_UI>().raceUI.SetActive(false);
           
            // mobileControls.SetActive(false);

            // Update award icons (gold , bronze silver) at race finish menu
            if (sortedPositions[0].Name == playerName)
                startUI.GetComponent<Start_Finish_UI>().Update_Award(0, levelID);
            else if (sortedPositions[1].Name == playerName)
                startUI.GetComponent<Start_Finish_UI>().Update_Award(1, levelID);
            else if (sortedPositions[2].Name == playerName)
                startUI.GetComponent<Start_Finish_UI>().Update_Award(2, levelID);
            else
                startUI.GetComponent<Start_Finish_UI>().Update_Award(3, levelID);

            yield return new WaitForSeconds(5f);

            startUI.GetComponent<Start_Finish_UI>().Hide_Award();

            startUI.SetActive(true);
            startUI.GetComponent<Start_Finish_UI>().finishRaceMenu.SetActive(true);

            startUI.GetComponent<Start_Finish_UI>().totalScores.text =
                "Total Coins : " +
                PlayerPrefs.GetInt("TotalScores").ToString();

            Update_Positions_Display();
        }

        void Update()
        {
            // playerInfo / lapInfo là UI tùy chọn — player dùng HUD riêng nên có thể để trống
            if (playerPosition != null)
            {
                if (playerInfo)
                    playerInfo.text = "Pos : " + (playerPosition.currentPosition + 1).ToString()
                    + " / " + carPositions.Length.ToString();

                if (playerPosition.currentLap > 0)
                {
                    if (lapInfo)
                        lapInfo.text = "Lap : " + playerPosition.currentLap.ToString()
                         + " / " + totalLaps.ToString();
                }
                else
                {
                    if (lapInfo)
                        lapInfo.text = "Lap : 1" + " / " + totalLaps.ToString();
                }
            }
            //_________________________________

            // Positions info
            for (int pos = 0; pos < racerInfo.Length; pos++)
            {
                if (racerInfo.Length != 0)
                {
                    if (racerInfo[pos])
                        racerInfo[pos].text = "   " + (pos + 1).ToString() + "   |   " + sortedPositions[pos].Name.ToString();
                }
            }
        }

        // List and sort car positions based on the istance form the checkpoints
        public void Update_Position(int racerID, string totalPoints)
        {
            // List and sort racer positions based on the distance from the checkpoint
            positions[racerID].Position = float.Parse(totalPoints);
            sortedPositions = positions.OrderBy(number => number.Position).ToList();

            sortedPositions.Reverse();
            //_________________________________

            if (playerPosition != null)
            {
                for (int b = 0; b < sortedPositions.Count; b++)
                {
                    if (playerPosition.RacerName == sortedPositions[b].Name)
                        playerPosition.currentPosition = b;
                }
            }

            // Enable current position icon (on the top of the car) for each racer
            for (int a = 0; a < carPositions.Length; a++)
            {
                for (int c = 0; c < carPositions.Length; c++)
                {
                    if (carPositions[a].RacerName == sortedPositions[c].Name)
                    {
                        carPositions[a].Update_Position(c);
                    }
                }
            }

            //_________________________________

        }
        public void Update_SideUI()
        {
            // Enable or disable right side position ui
            if (PlayerPrefs.GetString("Side_UI") == "On")
                positionUI.SetActive(true);
            else
                positionUI.SetActive(false);
        }
    }
}