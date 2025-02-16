using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Multiplayer.Playmode;
using UnityEngine;
using UnityEngine.SceneManagement;
using QFSW.QC;
using System.Linq;

public class ApplicationController : MonoBehaviour
{
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;

    private ApplicationData appData;

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);

        // Beacuse of dedicated server does not have graphics,
        // we can check the system to see if it is Dedicated Server or not
        await LaunchInMode(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null);
    }

    private async Task LaunchInMode(bool isDedicatedServer)
    {
        appData = new ApplicationData();

        if (isDedicatedServer)
        {
        }

        // Flow ClinetSingleton tries to Initialize the ClientManager and it tries to
        // authenticate with AuthenticationWrapper and check here if it is authenticated to continue
        else
        {
            // Here we took these two up. Beacuse we need some time to create HostSingleton object
            // and call the Start Method inside of it. Due to await for Client Singleton,
            // we have time to call Start method of Host Singleton.
            // If there is no await, there will be a race condition for Start Methods.
            HostSingleton hostSingleton = Instantiate(hostPrefab);
            hostSingleton.CreateHost();

            ClientSingleton clientSingleton = Instantiate(clientPrefab);
            bool authenticated = await clientSingleton.CreateClient();

            if (authenticated)
            {
                clientSingleton.GameManager.GoToMenu();
            }
        }

    }

    [Command()]
    public static void ChangeScene(string sceneId)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(sceneId, LoadSceneMode.Single);
    }
}
