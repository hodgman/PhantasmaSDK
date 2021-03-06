﻿using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour {

    public Text                 addressLabel, errorMessage;
    public Button               retryConnectionButton;

    // Menus
    public MyAssetsMenu         myAssetsMenu;
    public LoginMenu            loginMenu;
    public MainMenu             mainMenu;
    public AccountMenu          accountMenu;
    public MarketMenu           marketMenu;
    public AdminMenu            adminMenu;

    // Popups
    public NewKeyPopup          newKeyPopup;
    public OperationPopup       operationPopup;
    public ResultPopup          resultPopup;
    public CancelOperationPopup cancelOperationPopup;
    public BuyPopup             buyPopup;
    public SellPopup            sellPopup;
    public RemovePopup          removePopup;
    
    private static CanvasManager _instance;
    public static CanvasManager Instance
    {
        get { _instance = _instance == null ? FindObjectOfType(typeof(CanvasManager)) as CanvasManager : _instance; return _instance; }
    }

    // Use this for initialization
    void Start () {

        errorMessage.gameObject.SetActive(false);
        retryConnectionButton.gameObject.SetActive(false);

        loginMenu.gameObject.SetActive(false);
        mainMenu.gameObject.SetActive(false);
        accountMenu.gameObject.SetActive(false);
        myAssetsMenu.gameObject.SetActive(false);
        marketMenu.gameObject.SetActive(false);
        adminMenu.gameObject.SetActive(false);
    }
	
    public void SetAddress(string address)
    {
        addressLabel.text = address;
    }

    public void ClearAddress()
    {
        addressLabel.text = string.Empty;
    }

    public void SetErrorMessage(string error)
    {
        errorMessage.text = error;
        errorMessage.gameObject.SetActive(true);

        retryConnectionButton.gameObject.SetActive(true);
    }

    public void RetryConnectionClicked()
    {
        errorMessage.gameObject.SetActive(false);
        retryConnectionButton.gameObject.SetActive(false);

        PhantasmaDemo.Instance.LoadPhantasmaData();
    }

    #region Login Menu

    public void OpenLogin()
    {
        mainMenu.gameObject.SetActive(false);

        loginMenu.ClearAddress();
        loginMenu.gameObject.SetActive(true);
    }

    public void CloseLogin()
    {
        addressLabel.gameObject.SetActive(true);
        mainMenu.gameObject.SetActive(true);

        loginMenu.gameObject.SetActive(false);
    }

    #endregion

    #region Account Menu

    public void OpenAccount()
    {
        mainMenu.gameObject.SetActive(false);

        accountMenu.gameObject.SetActive(true);
    }

    public void CloseAccount()
    {
        mainMenu.gameObject.SetActive(true);

        accountMenu.gameObject.SetActive(false);
    }

    #endregion

    #region My Assets Menu

    public void OpenMyAssets()
    {
        addressLabel.gameObject.SetActive(false);
        mainMenu.gameObject.SetActive(false);

        myAssetsMenu.gameObject.SetActive(true);
    }

    public void CloseMyAssets()
    {
        addressLabel.gameObject.SetActive(true);
        mainMenu.gameObject.SetActive(true);

        myAssetsMenu.gameObject.SetActive(false);
    }

    #endregion
    
    #region Market Menu

    public void OpenMarket()
    {
        addressLabel.gameObject.SetActive(false);
        mainMenu.gameObject.SetActive(false);

        marketMenu.gameObject.SetActive(true);
    }

    public void CloseMarket()
    {
        addressLabel.gameObject.SetActive(true);
        mainMenu.gameObject.SetActive(true);

        marketMenu.gameObject.SetActive(false);
    }

    #endregion

    #region Admin Menu

    public void OpenAdmin()
    {
        mainMenu.gameObject.SetActive(false);
        addressLabel.gameObject.SetActive(false);

        adminMenu.gameObject.SetActive(true);
    }

    public void CloseAdmin()
    {
        addressLabel.gameObject.SetActive(true);
        mainMenu.gameObject.SetActive(true);

        adminMenu.gameObject.SetActive(false);
    }

    #endregion

    #region Popups

    public void ShowNewKeyPopup(string newKey)
    {
        newKeyPopup.ShowPopup(newKey);
    }

    public void HideNewKeyPopup()
    {
        newKeyPopup.HidePopup();
    }

    public void ShowOperationPopup(string message, bool showCancelButton)
    {
        operationPopup.ShowPopup(message, showCancelButton);
    }

    public void HideOperationPopup()
    {
        operationPopup.HidePopup();
    }

    public void ShowResultPopup(EOPERATION_RESULT type, string message)
    {
        resultPopup.ShowPopup(type, message);
    }

    public void HideResultPopup()
    {
        resultPopup.HidePopup();
    }

    public void ShowCancelOperationPopup(EOPERATION_RESULT type, string message)
    {
        cancelOperationPopup.ShowPopup(type, message);
    }

    public void HideCancelOperationPopup()
    {
        cancelOperationPopup.HidePopup();
    }

    public void ShowSellPopup(Car car)
    {
        sellPopup.SetPopup(car);
        sellPopup.gameObject.SetActive(true);
    }

    public void HideSellPopup()
    {
        sellPopup.gameObject.SetActive(false);
    }

    public void ShowBuyPopup(Car car)
    {
        buyPopup.SetPopup(car);
        buyPopup.gameObject.SetActive(true);
    }

    public void HideBuyPopup()
    {
        buyPopup.gameObject.SetActive(false);
    }

    public void ShowRemovePopup(Car car)
    {
        removePopup.SetPopup(car);
        removePopup.gameObject.SetActive(true);
    }

    public void HideRemovePopup()
    {
        removePopup.gameObject.SetActive(false);
    }

    #endregion

}
