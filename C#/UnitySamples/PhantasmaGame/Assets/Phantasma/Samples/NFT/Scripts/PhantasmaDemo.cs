﻿using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.IO;
using Phantasma.Numerics;
using Phantasma.SDK;
using Phantasma.VM.Utils;

using Random = UnityEngine.Random;
using Token = Phantasma.SDK.Token;

public enum EOPERATION_RESULT
{
    FAIL,
    SUCCESS
}

public class PhantasmaDemo : MonoBehaviour
{
    public enum EBLOCKCHAIN_OPERATION
    {
        CREATE_TOKEN,
        MINT_TOKEN,
        SELL_ASSET,
        BUY_ASSET,
        REMOVE_ASSET
    }

    private readonly Dictionary<EBLOCKCHAIN_OPERATION, string> _BLOCKCHAIN_OPERATION_DESCRIPTION = new Dictionary<EBLOCKCHAIN_OPERATION, string>
        {
            { EBLOCKCHAIN_OPERATION.CREATE_TOKEN,   "Create new token." },
            { EBLOCKCHAIN_OPERATION.MINT_TOKEN,     "Mint new token." },
            { EBLOCKCHAIN_OPERATION.SELL_ASSET,     "Sell asset on the market." },
            { EBLOCKCHAIN_OPERATION.BUY_ASSET,      "Buy asset from the market." },
            { EBLOCKCHAIN_OPERATION.REMOVE_ASSET,   "Remove asset from the market." },
        };

    public const string PHANTASMA_CHAIN = "main";

    public const string TOKEN_SYMBOL    = "CAR";
    public const string TOKEN_NAME      = "Car Demo Token";

    public const string SOUL_TOKEN_SYMBOL = "SOUL";

    private const string _SERVER_ADDRESS = "http://localhost:7077/rpc";

    private const float _TRANSACTION_CONFIRMATION_DELAY = 10f;
    
    public Market       market;
    public List<Sprite> carImages;

    private IEnumerator             _pendingTxCoroutine;
    private string                  _lastTransactionHash;
    private EBLOCKCHAIN_OPERATION   _lastTransactionType;

    public KeyPair                      Key                 { get; private set; }
    public API                          PhantasmaApi        { get; private set; }
    public Dictionary<string, Token>    PhantasmaTokens     { get; private set; }
    public bool                         IsTokenCreated      { get; private set; }
    public bool                         IsTokenOwner        { get; private set; }
    public decimal                      TokenCurrentSupply  { get; private set; }
    public Dictionary<string, Car>      MyCars              { get; set; }

    private static PhantasmaDemo _instance;
    public static PhantasmaDemo Instance
    {
        get { _instance = _instance == null ? FindObjectOfType(typeof(PhantasmaDemo)) as PhantasmaDemo : _instance; return _instance; }
    }

    private void Awake()
    {
        PhantasmaTokens = new Dictionary<string, Token>();
        MyCars          = new Dictionary<string, Car>();
    }

    private void Start ()
    {
        PhantasmaApi = new API(_SERVER_ADDRESS);
        
        Invoke("LoadPhantasmaData", 2f);
    }

    /// <summary>
    /// Load the tokens deployed on Phantasma Blockchain
    /// </summary>
    public void LoadPhantasmaData()
    {
        CheckTokens(() =>
        {
            CanvasManager.Instance.OpenLogin();
        });
    }

    /// <summary>
    /// Generate a new WIF
    /// </summary>
    /// <param name="callback"></param>
    public void GenerateNewKey(Action<string> callback = null)
    {
        var newKey = KeyPair.Generate().ToWIF();

        if (callback != null)
        {
            callback(newKey);
        }
    }

    /// <summary>
    /// Log in to Phantasma Blockchain
    /// </summary>
    /// <param name="privateKey">User private key</param>
    public void Login(string privateKey)
    {
        try
        {
            Key = KeyPair.FromWIF(privateKey);

            GetAccount(Key.Address.ToString());
        }
        catch (Exception e)
        {
            CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.FAIL, "Not valid Private Key: " + e.Message);   
        }
    }

    private void LoggedIn(string address)
    {
        CanvasManager.Instance.SetAddress(address);
        CanvasManager.Instance.CloseLogin();
    }

    public void LogOut()
    {
        IsTokenCreated  = false;
        IsTokenOwner    = false;
        
        TokenCurrentSupply = 0;

        MyCars.Clear();

        CanvasManager.Instance.mainMenu.SetAdminButton();

        CanvasManager.Instance.myAssetsMenu.ClearContent();
        CanvasManager.Instance.marketMenu.ClearContent();

        CanvasManager.Instance.ClearAddress();
        CanvasManager.Instance.OpenLogin();
    }

    #region Blockchain calls

    public IEnumerator CheckOperation(EBLOCKCHAIN_OPERATION operation, string transactionHash, Action<Transaction> callback, Action<EPHANTASMA_SDK_ERROR_TYPE, string> errorHandlingCallback = null)
    {
        if (_pendingTxCoroutine != null)
        {
            StopCoroutine(_pendingTxCoroutine);

            _pendingTxCoroutine = null;
        }

        _pendingTxCoroutine = CheckOperationCoroutine(operation, transactionHash, callback, errorHandlingCallback);

        yield return StartCoroutine(_pendingTxCoroutine);
    }

    private IEnumerator CheckOperationCoroutine(EBLOCKCHAIN_OPERATION operation, string transactionHash, Action<Transaction> callback, Action<EPHANTASMA_SDK_ERROR_TYPE, string> errorHandlingCallback = null)
    {
        _lastTransactionType = operation;
        _lastTransactionHash = transactionHash;

        var isTransactionCompleted = false;

        while (!isTransactionCompleted)
        {
            yield return PhantasmaApi.GetTransaction(transactionHash,
                (tx) =>
                {
                    isTransactionCompleted = true;

                    if (callback != null)
                    {
                        callback(tx);
                    }
                },
                (errorType, errorMessage) =>
                {
                    if (errorType == EPHANTASMA_SDK_ERROR_TYPE.API_ERROR && errorMessage.Equals("pending"))
                    {
                        // Pending
                    }
                    else
                    {
                        isTransactionCompleted = true;

                        if (errorHandlingCallback != null)
                        {
                            errorHandlingCallback(errorType, errorMessage);
                        }
                    }
                });

            yield return new WaitForSecondsRealtime(_TRANSACTION_CONFIRMATION_DELAY);
        }
    }

    public void CancelTransaction()
    {
        StartCoroutine(CancelTransactionCoroutine(_lastTransactionHash));
    }

    private IEnumerator CancelTransactionCoroutine(string transactionHash)
    {
        yield return PhantasmaApi.CancelTransaction(transactionHash,
            (tx) =>
            {
                if (_pendingTxCoroutine != null)
                {
                    StopCoroutine(_pendingTxCoroutine);

                    _pendingTxCoroutine = null;
                }

                CanvasManager.Instance.HideOperationPopup();
                CanvasManager.Instance.HideResultPopup();
                CanvasManager.Instance.ShowCancelOperationPopup(EOPERATION_RESULT.SUCCESS, "The operation '" + _BLOCKCHAIN_OPERATION_DESCRIPTION[_lastTransactionType] + "' was canceled with success.");
            },
            (errorType, errorMessage) =>
            {
                CanvasManager.Instance.ShowCancelOperationPopup(EOPERATION_RESULT.FAIL, 
                    "The transaction regarding the operation '" + _BLOCKCHAIN_OPERATION_DESCRIPTION[_lastTransactionType] + "' is already being processed by the blockchain and cannot be canceled anymore.");
            });
    }

    /// <summary>
    /// Returns the account name and balance of given address.
    /// </summary>
    /// <param name="address">String, base58 encoded - address to check for balance and name.</param>
    public void GetAccount(string address)
    {
        CanvasManager.Instance.ShowOperationPopup("Fetching account data from the blockchain...", false);

        StartCoroutine(PhantasmaApi.GetAccount(address, 
            account =>
            {
                CanvasManager.Instance.accountMenu.SetBalance("Name: " + account.name);

                foreach (var balance in account.balances)
                {
                    var isFungible = PhantasmaTokens[balance.symbol].flags.Contains("Fungible");

                    var amount = isFungible ? decimal.Parse(balance.amount) / (decimal) Mathf.Pow(10f, 8) : decimal.Parse(balance.amount);
                    CanvasManager.Instance.accountMenu.AddBalanceEntry("Chain: " + balance.chain + " - " + amount + " " + balance.symbol);

                    if (balance.symbol.Equals(TOKEN_SYMBOL))
                    {
                        TokenCurrentSupply = amount;

                        MyCars.Clear();

                        foreach (var tokenID in balance.ids)
                        {
                            StartCoroutine(PhantasmaApi.GetTokenData(TOKEN_SYMBOL, tokenID.ToString(), 
                                (tokenData =>
                                {
                                    var ramBytes        = Base16.Decode(tokenData.ram);
                                    var carMutableData  = Serialization.Unserialize<CarMutableData>(ramBytes);
                                    
                                    var romBytes    = Base16.Decode(tokenData.rom);
                                    var carData     = Serialization.Unserialize<CarData>(romBytes);

                                    var newCar = new Car();
                                    newCar.SetCar(Address.FromText(tokenData.ownerAddress), tokenID, carData, carMutableData);

                                    MyCars.Add(newCar.TokenID, newCar);
                                }),
                                (type, s) =>
                                {

                                }));
                        }
                    }
                }

                CanvasManager.Instance.HideOperationPopup();

                LoggedIn(address);

            },
            (errorType, errorMessage) =>
            {
                CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.FAIL, errorType + " - " + errorMessage);
            }
        ));
    }

    /// <summary>
    /// Create a new token on Phantasma Blockchain
    /// </summary>
    public void CreateToken()
    {
        CheckTokens(() =>
        {
            CanvasManager.Instance.ShowOperationPopup("Creating a new token on the blockchain...", false);

            var script = ScriptUtils.BeginScript()
                .AllowGas(Key.Address, Address.Null, 1, 9999)
                .CallContract("nexus", "CreateToken", Key.Address, TOKEN_SYMBOL, TOKEN_NAME, 10000, 0, TokenFlags.Transferable | TokenFlags.Finite)
                .SpendGas(Key.Address)
                .EndScript();

            StartCoroutine(PhantasmaApi.SignAndSendTransaction(Key, script, "main",
                (result) =>
                {
                    StartCoroutine(CheckTokenCreation(EBLOCKCHAIN_OPERATION.CREATE_TOKEN, result));
                },
                (errorType, errorMessage) =>
                {
                    CanvasManager.Instance.HideOperationPopup();
                    CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.FAIL, errorType + " - " + errorMessage);
                }
            ));
        });
    }

    /// <summary>
    /// Check if the creation of a new token on Phantasma Blockchain was successful
    /// </summary>
    public IEnumerator CheckTokenCreation(EBLOCKCHAIN_OPERATION operation, string result)
    {
        CanvasManager.Instance.ShowOperationPopup("Checking token creation...", true);

        yield return CheckOperation(operation, result, 
            (tx) =>
            {
                foreach (var evt in tx.events)
                {
                    EventKind eKind;
                    if (Enum.TryParse(evt.kind, out eKind))
                    {
                        if (eKind == EventKind.TokenCreate)
                        {
                            var bytes       = Base16.Decode(evt.data);
                            var tokenSymbol = Serialization.Unserialize<string>(bytes);

                            Debug.Log(evt.kind + " - " + tokenSymbol);

                            if (tokenSymbol.Equals(TOKEN_SYMBOL))
                            {
                                IsTokenCreated  = true;
                                IsTokenOwner    = true;

                                CheckTokens(() =>
                                {
                                    CanvasManager.Instance.adminMenu.SetContent();
                                });

                                CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.SUCCESS, "New token created with success.");
                            }

                            return;
                        }
                    }
                }

                CanvasManager.Instance.HideOperationPopup();
                CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.FAIL, "Something failed on the connection to the blockchain. Please try again.");
            },
            ((errorType, errorMessage) =>
            {
                CanvasManager.Instance.HideOperationPopup();
                CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.FAIL, errorType + " - " + errorMessage);
            }));
    }

    /// <summary>
    /// Check the tokens deployed in Phantasma Blockchain.
    /// </summary>
    public void CheckTokens(Action callback = null)
    {
        IsTokenCreated = false;

        CanvasManager.Instance.ShowOperationPopup("Fetching Phantasma tokens...", false);
        
        PhantasmaTokens.Clear();

        StartCoroutine(PhantasmaApi.GetTokens(
            (result) =>
            {
                foreach (var token in result)
                {
                    PhantasmaTokens.Add(token.symbol, token);

                    if (token.symbol.Equals(TOKEN_SYMBOL))
                    {
                        IsTokenCreated = true;
                        break;
                    }
                }

                CanvasManager.Instance.HideOperationPopup();

                if (callback != null)
                {
                    callback();
                }
            },
            (errorType, errorMessage) =>
            {
                CanvasManager.Instance.HideOperationPopup();

                if (CanvasManager.Instance.loginMenu.gameObject.activeInHierarchy)
                {
                    CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.FAIL, errorType + " - " + errorMessage);
                }
                else if (CanvasManager.Instance.adminMenu.gameObject.activeInHierarchy)
                {
                    CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.FAIL, errorType + " - " + errorMessage);
                }
                else
                {
                    CanvasManager.Instance.SetErrorMessage(errorType + " - "  + errorMessage);
                }
            }
        ));
    }

    /// <summary>
    /// Check if the logged in address is the owner of a token
    /// </summary>
    /// <param name="tokenSymbol">Symbol of the token</param>
    public bool OwnsToken(string tokenSymbol, Action callback = null)
    {
        IsTokenOwner = false;

        CanvasManager.Instance.ShowOperationPopup("Fetching tokens from the blockchain...", false);

        StartCoroutine(PhantasmaApi.GetTokens(
            (result) =>
            {
                foreach (var token in result)
                {
                    if (token.symbol.Equals(TOKEN_SYMBOL) && token.ownerAddress.Equals(Key.Address.ToString()))
                    {
                        IsTokenOwner = true;
                        break;
                    }
                }

                CanvasManager.Instance.HideOperationPopup();

                if (callback != null)
                {
                    callback();
                }
            },
            (errorType, errorMessage) =>
            {
                CanvasManager.Instance.HideOperationPopup();
                CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.FAIL, errorType + " - " + errorMessage);
            }
        ));

        return IsTokenOwner;
    }

    /// <summary>
    /// Mint a new token and increase the supply of the created token
    /// </summary>
    public void MintToken(string tokenName)
    {
        var carData = new CarData
        {
            rarity  = CarRarity.Common,
            imageID = Random.Range(0, carImages.Count)
        };

        var carMutableData = new CarMutableData
        {
            name        = tokenName,
            power       = (byte)Random.Range(1, 10),
            speed       = (byte)Random.Range(1, 10),
            location    = CarLocation.None,
        };

        var txData          = Serialization.Serialize(carData);
        var txMutableData   = Serialization.Serialize(carMutableData);
        
        var script = ScriptUtils.BeginScript()
                        .AllowGas(Key.Address, Address.Null, 1, 9999)
                        .CallContract("token", "MintToken", Key.Address, TOKEN_SYMBOL, txData, txMutableData, 0)
                        .SpendGas(Key.Address)
                        .EndScript();
        
        CanvasManager.Instance.ShowOperationPopup("Minting a new token...", false);

        StartCoroutine(PhantasmaApi.SignAndSendTransaction(Key, script, "main",
            (result) =>
            {
                StartCoroutine(CheckTokenMint(carData, carMutableData, result));
            },
            (errorType, errorMessage) =>
            {
                CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.FAIL, errorType + " - " + errorMessage);
            }
        ));
    }

    /// <summary>
    /// Check if the auction purchase was successful
    /// </summary>
    private IEnumerator CheckTokenMint(CarData carData, CarMutableData carMutableData, string result)
    {
        CanvasManager.Instance.ShowOperationPopup("Checking token mint...", true);

        yield return CheckOperation(EBLOCKCHAIN_OPERATION.MINT_TOKEN, result,
            (tx) =>
            {
                foreach (var evt in tx.events)
                {
                    EventKind eKind;
                    if (Enum.TryParse(evt.kind, out eKind))
                    {
                        if (eKind == EventKind.TokenMint)
                        {
                            var bytes       = Base16.Decode(evt.data);
                            var tokenData   = Serialization.Unserialize<TokenEventData>(bytes);

                            var tokenID = tokenData.value;

                            var newCar = new Car();
                            newCar.SetCar(Key.Address, tokenID.ToString(), carData, carMutableData);

                            // Add new car to admin assets
                            MyCars.Add(tokenID.ToString(), newCar);

                            CheckTokens(() => { CanvasManager.Instance.adminMenu.SetContent(); });

                            CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.SUCCESS, "Token mint with success.");

                            return;
                        }
                    }
                }

                CanvasManager.Instance.HideOperationPopup();
                CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.FAIL, "Something failed while executing a new token mint. Please try again.");
            },
            ((errorType, errorMessage) =>
            {
                CanvasManager.Instance.HideOperationPopup();
                CanvasManager.Instance.ShowResultPopup(EOPERATION_RESULT.FAIL, errorType + " - " + errorMessage);
            }));
    }

    #endregion
}
