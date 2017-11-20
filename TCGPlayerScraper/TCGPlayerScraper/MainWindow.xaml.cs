using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Linq;
using System.Windows;
using MySql.Data.MySqlClient;


namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        static IWebDriver driver;
        public MainWindow()
        {
            this.Hide();    //Hide the MainWindow for it is not used yet
            fetchDataSetsFromTCG();     //Call helper method to scrape site info
        }

        private void fetchDataSetsFromTCG()
        {
            int setCount = 1;
            driver = new ChromeDriver(@"C:\chromedriver_win32");    //Initialize the Selenium Chrome Driver
            driver.Navigate().GoToUrl("https://shop.tcgplayer.com/magic?partner=MTGTCG");   //Navigate to TCG's MTG "All Sets" page
            var setList = driver.FindElements(By.ClassName("search-list"));     //Find all elements with search-list and add to setList
            MySqlConnection myConnector = new MySqlConnection("server=localhost;user id=Frodo;password=m00pd00pb00p;database=mtgcarddb;");
            try
            {
                myConnector.Open();         //Open connetion to MySQL Database
            }
            catch(Exception e)
            {
                Console.WriteLine("\n\nError while opening connection!\n\n");
                Console.WriteLine(e);
            }

            foreach (var webElement in setList)
            {
                String text = webElement.Text;  //Obtain the text from the element
                String setName = "defaultValue";
                if (text.Contains("th Edition") == true) {
                    text = switchOrdinalCase(text);     //TCG Summary Page uses 10th, 9th, 8th, etc.; not Tenth, Ninth, Eighth
                }
                else if (text.Equals("Amonkhet Invocations"))
                {
                    text = "Masterpiece+Series%3A+Amonkhet+Invocations";        //TCG uses special encoding for Amonkhet Masterpieces
                }
                else if (text.Equals("Kaladesh Inventions"))
                {
                    text = "Masterpiece+Series%3A+Kaladesh+Inventions";         //TCG uses special encoding for Kaladesh Inventions
                }
                else if (text.Equals("From the Vault: Transform"))
                {
                    continue;       //Skip this iteration, set has not released yet
                }
                else if (text.Equals("Modern Event Deck"))
                {
                    text = Uri.EscapeUriString("Magic Modern Event Deck");
                }
                else if (text.Contains("vs."))
                {
                    text = fixDuelDecksText(text);        //TCG is inconsistent with the Duel Decks on Advanced Search
                }
                else if (text.Contains("PDS: "))
                {
                    text = Uri.EscapeUriString(text.Replace("PDS: ", "Premium Deck Series: "));      //TCG spells out PDS on the Advanced Search Page
                }
                else if (text.Equals("Magic the Gathering: Gift Pack 2017"))
                {
                    continue;       //Skip this iteration, set has not released yet
                }
                else if (text.Equals("WPN & Gateway Promos"))
                {
                    text = "WPN%2FGateway+Promos";      //TCG encodes this set differently
                }
                else if (text.Equals("Launch Party & Release Event Cards"))
                {
                    text = "Launch+Party+%26+Release+Event+Promos";      //TCG encodes this set differently
                }
                else if (text.IndexOf(" ") != -1)     //-1 is for when the character is not found
                {
                    text = Uri.EscapeUriString(text);       //URI encode the text for future navigation
                }
                
                Console.WriteLine(text);     //Actually print out each set on the console
                
                String newUrl = "http://magic.tcgplayer.com/db/search_result.asp?Set_Name=";        //Standard start for TCG Set Summary Page
                newUrl += text;     //Add the URI Encoded Set to the end of the URL

                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;       //Create obeject to execute TCG Sort Function and open tabs
                js.ExecuteScript("window.open()");
                driver.SwitchTo().Window(driver.WindowHandles.Last());     //Switch back to main tab

                driver.Navigate().GoToUrl(newUrl);      //Navigate to the new URL
                
                js.ExecuteScript("SortOrder('MeanPrice DESC');");       //TCG method to sort set by Average Card Value in Descending Order
                var cardData = driver.FindElements(By.ClassName("default_8"));
                

                //Loop extracts card data from webpage, prints to console, and inserts into SQL Database
                for(int i=8; i< cardData.Count;)
                {
                    var webElem = cardData[i];
                    if (i < (cardData.Count-4))
                    {
                        String cardName = webElem.Text;
                        i++;
                        webElem = cardData[i];
                        String cmc = webElem.Text;
                        i++;
                        webElem = cardData[i];
                        setName = webElem.Text;
                        i++;
                        webElem = cardData[i];
                        char rarity = webElem.Text[0];
                        i++;
                        webElem = cardData[i];
                        String num = webElem.Text;
                        if (webElem.Text.Equals("N/A"))
                            num = "0";
                        double highPrice = Convert.ToDouble(num.Replace("$", ""));
                        i++;
                        webElem = cardData[i];
                        num = webElem.Text;
                        if (webElem.Text.Equals("N/A"))
                            num = "0";
                        double medPrice = Convert.ToDouble(num.Replace("$", ""));
                        i++;
                        webElem = cardData[i];
                        num = webElem.Text;
                        if (webElem.Text.Equals("N/A"))
                            num = "0";
                        double lowPrice = Convert.ToDouble(num.Replace("$", ""));
                        i++;
                        Console.WriteLine(cardName + "   " + cmc + "   " + setCount + "   " + rarity + "   " + highPrice + "   " + medPrice + "   " + lowPrice);
                        string sqlCardCmd = "insert into cards (cardName, convertedManaCost, setName, rarity, highPrice, medPrice, lowPrice) values (@cardName, @cmc," + setCount + ",@rarity," + highPrice + "," + medPrice + "," + lowPrice + ");";
                        MySqlCommand cardCmd = new MySqlCommand(sqlCardCmd, myConnector);
                        cardCmd.Parameters.AddWithValue("@cardName", cardName);
                        cardCmd.Parameters.AddWithValue("@cmc", cmc);
                        cardCmd.Parameters.AddWithValue("@rarity", rarity);
                        cardCmd.ExecuteNonQuery();
                        
                    }
                    else
                    {
                        break;
                        //Need to skip the last 4 elements: Color, Deck Name, Creator, and Format
                    }
                    
                }
                
                js.ExecuteScript("window.close()");     //Close the tab
           
                driver.SwitchTo().Window(driver.WindowHandles.First());     //Switch back to main tab
                string sqlSetCmd = "insert into sets (setName, id) values (@setName," + setCount + ");";
                MySqlCommand setCmd = new MySqlCommand(sqlSetCmd, myConnector);
                setCmd.Parameters.AddWithValue("@setName", setName);
                setCmd.ExecuteNonQuery();
                setCount++;
            }

            System.Windows.Application.Current.Shutdown();      //Shutdown the application
            try
            {
                myConnector.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\nError while closing connection!\n\n");
                Console.WriteLine(e);
            }
        }

        private String switchOrdinalCase(String str)
        {
            if (str.Contains("Tenth"))
            {
                str = "10";
            }
            else if (str.Contains("Ninth"))
            {
                str = "9";
            }
            else if (str.Contains("Eighth"))
            {
                str = "8";
            }
            else if (str.Contains("Seventh"))
            {
                str = "7";
            }
            else if (str.Contains("Sixth"))
            {
                str = "Classic Six";        //TCG uses Classic Sixth Edition instead of 6th Edition
            }
            else if (str.Contains("Fifth"))
            {
                str = "Fif";        //TCG uses Fifth Edition, does not need changed
            }
            else if (str.Contains("Fourth"))
            {
                str = "Four";       //TCG uses Fourth Edition, does not need changed
            }
            str += "th Edition";     //Magic only has 4th to 10th Editions
            return str;     //Return the new Ordinal (4th, 5th, 6th, etc...)
        }

        private String fixDuelDecksText(String str)
        {
            if (str.Contains("Duel Decks: "))
            {
                return Uri.EscapeUriString(str);        //No changes need made to any of these, only need to add "Duel Decks: "
            }
            else
            {
                return Uri.EscapeUriString("Duel Decks: " + str);       //Other sets need "Duel Decks: " added as prefix
            }        }
    }
}
