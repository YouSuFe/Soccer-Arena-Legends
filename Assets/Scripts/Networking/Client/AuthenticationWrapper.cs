using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public enum AuthState
{
    NotAuthenticated,
    Authenticating,
    Authenticated,
    Error,
    TimeOut
}

public static class AuthenticationWrapper
{
    public static AuthState AuthState { get; private set; } = AuthState.NotAuthenticated;

    // UGS will try to make it authenticated with number of tries
    public static async Task<AuthState> DoAuth(int maxTries = 5)
    {
        // If it is already authenticated
        if (AuthState == AuthState.Authenticated) return AuthState;

        // What if two script or two thing want to Authentication? To prevent this we did
        if(AuthState == AuthState.Authenticating)
        {
            Debug.LogWarning("Already authenticating!");
            await AuthenticatingAsync();
            return AuthState;
        }

        await SignInAnonymouslyAsync(maxTries);

        return AuthState;
    }

    private static async Task<AuthState> AuthenticatingAsync()
    {
        while(AuthState == AuthState.Authenticating || AuthState == AuthState.NotAuthenticated)
        {
            await Task.Delay(200);
        }

        return AuthState;
    }

    private static async Task SignInAnonymouslyAsync(int maxTries = 5)
    {
        AuthState = AuthState.Authenticating;

        int authTries = 0;

        while (AuthState == AuthState.Authenticating && authTries < maxTries)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
                {
                    AuthState = AuthState.Authenticated;
                    break;
                }
            }
            // Authentication Problem
            catch(AuthenticationException authException)
            {
                Debug.LogError(authException);
                AuthState = AuthState.Error;
            }
            // Internet Connection problem
            catch(RequestFailedException requestException)
            {
                Debug.LogError(requestException);
                AuthState = AuthState.Error;
            }


            authTries++;

            // We need to wait some time,
            // Otherwise we'll hit a rate limit because it'll happen and then it'll immediately try and happen again.
            // So, it is recommended on here to wait a millisecond before trying again.
            await Task.Delay(1000);
        }

        if(AuthState != AuthState.Authenticated)
        {
            Debug.LogWarning($"Player was not signed in successfully after {authTries} tries");
            AuthState = AuthState.TimeOut;
        }
    }

}


