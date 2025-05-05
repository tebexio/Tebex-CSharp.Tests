using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tebex.Common;
using Tebex.Plugin;
using Tebex.PluginAPI;

namespace Tebex_Unity.Tests.Tebex.PluginAPI
{
    [TestClass]
    public class PluginApiTests
    {
        private TebexPluginApi _pluginApi;
        private static TaskCompletionSource<bool> completion;
        private static string TestSecretKey = "key"; //TODO your secret key
        private static string TestGiftCardCode = "1234567890";
        private static string TestCreatorCode = "TebexDev";
        private static string TestUsername = "TebexDev";
        private static string TestEmail = "tebex-integrations@overwolf.com";
        private static string TestCouponCode = "Academy10";
        private static int TestPlayerId = 1;
        private static string TestPlayerUuid = "9e65a968ee4743d19a2a4c9969154491";
        private static string TestTransactionId = "tbx-4014225a79732-70a85f";
        private static int TestPackageId = 6051250;
        private static int TestGiftCardId = 1138835;
        
        Action<PluginApiError> _defaultTestApiError = apiError =>
        {
            try
            {
                Assert.Fail("API error: " + apiError.ErrorMessage);
            }
            catch (Exception ex)
            {
                completion.SetResult(false);
                completion.SetException(ex);
            }
        };
        Action<ServerError> _defaultTestServerError = serverError =>
        {
            try
            {
                Assert.Fail("Server error: " + serverError.Code + " " + serverError.Body);
            }
            catch (Exception ex)
            {
                completion.SetResult(false);
                completion.SetException(ex);
            }
        };
        
        private static void TestCompletableFuture(Task task)
        {
            try
            {
                task.Wait();
                completion.Task.Wait(); // Await the TaskCompletionSource to wait for the result
                Assert.IsTrue(completion.Task.IsCompleted, "The task is completed.");
                Assert.IsTrue(completion.Task.Exception == null, "The task has no exception");
                Assert.IsTrue(completion.Task.Result, "The task completed successfully.");
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception: " + ex.Message);
            }
        }
        
        [TestInitialize]
        public void TestSetup()
        {
            _pluginApi = TebexPluginApi.Initialize(new StandardPluginAdapter(), TestSecretKey);
            completion = new TaskCompletionSource<bool>();
        }

        private void TestPackage(Package package)
        {
            Assert.IsTrue(package.Id > 0, "Package ID should not be negative");
            Assert.IsTrue(package.Name.Length > 0, "Package name should not be empty");
            Assert.IsTrue(package.Order >= -1, "Package order should be valid");
            Assert.IsNotNull(package.Image);
            Assert.IsTrue(package.Price > 0, "Package price should not be negative");
            if (package.Sale != null)
            {
                Assert.IsTrue(package.Sale.Discount > 0.0d, "Package sale discount should not be negative");
            }
            Assert.IsTrue(package.ExpiryPeriod.Length > 0, "Package expiry period should not be empty");
            Assert.IsTrue(package.Type.Length > 0, "Package type should not be empty");
            Assert.IsNotNull(package.Category);
            TestCategory(package.Category);
            Assert.IsTrue(package.GlobalLimitPeriod.Length > 0, "Package global limit period should not be empty");
            Assert.IsTrue(package.UserLimitPeriod.Length > 0, "Package user limit period should not be empty");

            if (package.Servers != null)
            {
                foreach (var server in package.Servers)
                {
                    Assert.IsTrue(server.Id > 0, "Server ID should not be negative");
                    Assert.IsTrue(server.Name.Length > 0, "Server name should not be empty");
                }
            }
            
            Assert.IsNotNull(package.ShowUntil);
        }

        private void TestCategory(Category category)
        {
            Assert.IsTrue(category.Id > 0, "Category ID should not be negative.");
            Assert.IsNotNull(category.Packages, "Packages should not be null");
            Assert.IsTrue(category.Name.Length > 0, "Category name should not be empty.");
            Assert.IsNotNull(category.Subcategories, "Subcategories should not be null");
            if (category.Subcategories.Count > 0)
            {
                foreach (var sub in category.Subcategories)
                {
                    TestCategory(sub);
                }
            }
        }

        private void TestDuePlayer(DuePlayer player)
        {
            Assert.IsTrue(player.Id > 0, "Player ID should not be negative.");
            Assert.IsTrue(player.Name.Length > 0, "Player name should not be empty.");
            Assert.IsTrue(player.Uuid.Length > 0, "Player UUID should not be empty.");
        }
        
        [TestMethod]
        public void TestGetListing()
        {
            TestCompletableFuture(_pluginApi.GetListings(listingResponse =>
            {
                Assert.IsNotNull(listingResponse, "Listing response should not be null.");
                Assert.IsNotNull(listingResponse.Categories, "Categories list should not be null.");
                Assert.IsTrue(listingResponse.Categories.Count >= 0, "Categories count should not be negative.");
                foreach (var category in listingResponse.Categories)
                {
                    TestCategory(category);
                }
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestSendJoinEvents()
        {
            var events = new List<JoinEvent> { new JoinEvent("xndir", "server.join", "127.0.0.1") };
            TestCompletableFuture(_pluginApi.SendJoinEvents(events,
                onSuccess: res =>
                {
                    completion.SetResult(true);
                }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetInformation()
        {
            TestCompletableFuture(_pluginApi.GetInformation(store =>
            {
                Assert.IsNotNull(store);
                Assert.IsNotNull(store.Account);
                Assert.IsNotNull(store.Server);
                
                Assert.IsTrue(store.Account.Id > 0, "Account ID should not be negative.");
                Assert.IsTrue(store.Account.Name.Length > 0, "Account name should not be empty.");
                Assert.IsNotNull(store.Account.Currency);
                Assert.IsTrue(store.Account.Currency.Symbol.Length > 0, "Currency symbol should be present.");
                Assert.IsTrue(store.Account.Currency.Iso4217.Length > 0, "Currency ISO 4217 code should be present.");
                Assert.IsTrue(store.Account.Domain.Length > 0, "Domain should be present.");
                Assert.IsTrue(store.Account.GameType.Length > 0, "Game type should be present.");
                
                Assert.IsTrue(store.Server.Id > 0, "Server ID should not be negative.");
                Assert.IsTrue(store.Server.Name.Length > 0, "Server name should not be empty.");
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetCommandQueue()
        {
            TestCompletableFuture(_pluginApi.GetCommandQueue(queue =>
            {
                Assert.IsNotNull(queue);
                Assert.IsNotNull(queue.Meta);
                Assert.IsTrue(queue.Meta.NextCheck > 0, "Next check should not be negative.");
                Assert.IsNotNull(queue.Players, "Players list should not be null.");
                if (queue.Players.Count > 0)
                {
                    foreach (var duePlayer in queue.Players)
                    {
                        TestDuePlayer(duePlayer);
                    }
                }
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetOfflineCommands()
        {
            TestCompletableFuture(_pluginApi.GetOfflineCommands(cmds =>
            {
                Assert.IsNotNull(cmds);
                Assert.IsNotNull(cmds.Commands);
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetOnlineCommands()
        {
            TestCompletableFuture(_pluginApi.GetOnlineCommands(TestPlayerId, resp =>
            {
                Assert.IsNotNull(resp);
                Assert.IsNotNull(resp.Commands);
                Assert.IsNotNull(resp.Player);
                Assert.IsTrue(resp.Player.Id.Length > 0, "Player ID should not be negative.");
                Assert.IsTrue(resp.Player.Username.Length > 0, "Player username should not be empty.");
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestDeleteCommands()
        {
            TestCompletableFuture(_pluginApi.DeleteCommands(new[] { 1, 2 }, res =>
            {
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetAllPackages()
        {
            TestCompletableFuture(_pluginApi.GetAllPackages(true, packages =>
            {
                Assert.IsNotNull(packages);
                foreach (var package in packages)
                {
                    TestPackage(package);
                }
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetPackage()
        {
            TestCompletableFuture(_pluginApi.GetAllPackages(true, packages =>
            {
                var package = packages[0];
                _pluginApi.GetPackage(package.Id, pkg =>
                {
                    TestPackage(pkg);
                    completion.SetResult(true);
                }, _defaultTestApiError, _defaultTestServerError);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        private void TestCommunityGoal(CommunityGoal goal)
        {
            Assert.IsNotNull(goal, "The community goal object should not be null.");
            Assert.IsTrue(goal.Id > 0, "The ID should be a positive integer.");
            Assert.IsTrue(goal.CreatedAt > DateTime.MinValue, "CreatedAt should be a valid date.");
            Assert.IsTrue(goal.UpdatedAt >= goal.CreatedAt, "UpdatedAt should not be earlier than CreatedAt.");
            Assert.IsTrue(goal.Account > 0, "The Account ID should be a positive integer.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(goal.Name), "The Name should not be null or empty.");
            Assert.IsNotNull(goal.Description, "The Description should not be null.");
            Assert.IsNotNull(goal.Image, "The Image property should not be null.");
            Assert.IsTrue(goal.Target >= 0, "The Target value should not be negative.");
            Assert.IsTrue(goal.Current >= 0, "The Current value should not be negative.");
            Assert.IsTrue(goal.Current <= goal.Target, "The Current value should not exceed the Target value.");
            Assert.IsTrue(goal.Repeatable == 0 || goal.Repeatable == 1, "Repeatable should be 0 or 1.");
            if (goal.LastAchieved.HasValue)
            {
                Assert.IsTrue(goal.LastAchieved.Value > DateTime.MinValue, "LastAchieved should be a valid date if set.");
            }
            Assert.IsTrue(goal.TimesAchieved >= 0, "TimesAchieved should not be negative.");
            Assert.IsTrue(goal.Status.Length > 0, "Status should not be empty.");
            Assert.IsTrue(goal.Sale >= 0, "Sales value should not be negative.");
        }

        [TestMethod]
        public void TestGetCommunityGoals()
        {
            TestCompletableFuture(_pluginApi.GetAllCommunityGoals(goals =>
            {
                Assert.IsNotNull(goals);
                foreach (CommunityGoal goal in goals)
                {
                   TestCommunityGoal(goal);
                }
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetCommunityGoal()
        {
            TestCompletableFuture(_pluginApi.GetAllCommunityGoals(goals =>
            {
                Assert.IsNotNull(goals);
                if (goals.Count > 0)
                {
                    var goal = goals[0];
                    _pluginApi.GetCommunityGoal(goal.Id, responseGoal =>
                    {
                        TestCommunityGoal(responseGoal);
                        completion.SetResult(true);                        
                    }, _defaultTestApiError, _defaultTestServerError);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetAllPayments()
        {
            TestCompletableFuture(_pluginApi.GetAllPayments(10, payments =>
                {
                    Assert.IsNotNull(payments);
                    foreach (var payment in payments)
                    {
                        Assert.IsTrue(payment.Id > 0, "Payment ID should not be negative.");
                        Assert.IsNotNull(payment.Currency, "Payment currency should not be null.");
                        Assert.IsNotNull(payment.Gateway, "Payment gateway should not be null.");
                        Assert.IsTrue(payment.Status.Length > 0, "Payment status should not be empty.");
                        Assert.IsNotNull(payment.Player, "Payment player should not be null.");
                        Assert.IsNotNull(payment.Packages, "Payment packages should not be null.");
                        Assert.IsNotNull(payment.Notes, "Payment notes should not be null.");
                    }
                    completion.SetResult(true);
                }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetAllPaymentsPaginated()
        {
            TestCompletableFuture(_pluginApi.GetAllPaymentsPaginated(1, info =>
            {
                Assert.IsTrue(info.PerPage >= 0, "PerPage should be greater than or equal to 0.");
                Assert.IsTrue(info.Total >= 0, "Total payments should be greater than or equal to 0.");
                Assert.IsTrue(info.To >= 0, "To should be greater than or equal to 0.");
                Assert.IsTrue(info.From >= 0, "From should be greater than or equal to 0.");
                Assert.IsTrue(info.CurrentPage >= 0, "CurrentPage should be greater than or equal to 0.");
                Assert.IsTrue(info.LastPage >= 0, "LastPage should be greater than or equal to 0.");
                Assert.IsNotNull(info.Data, "Data should not be null.");
                Assert.IsTrue(info.Data.Count > 0, "Data should not be empty.");
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetPayment()
        {
            TestCompletableFuture(_pluginApi.GetPayment(TestTransactionId, payment =>
            {
                Assert.IsTrue(payment.Id > 0, "Payment ID should not be negative.");
                Assert.IsNotNull(payment.Currency, "Payment currency should not be null.");
                Assert.IsNotNull(payment.Gateway, "Payment gateway should not be null.");
                Assert.IsTrue(payment.Status.Length > 0, "Payment status should not be empty.");
                Assert.IsTrue(payment.Email.Length > 0, "Payment email should not be empty.");
                Assert.IsNotNull(payment.Player, "Payment player should not be null.");
                Assert.IsNotNull(payment.Packages, "Payment packages should not be null.");
                Assert.IsNotNull(payment.Notes, "Payment notes should not be null.");
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestCreateCheckoutUrl()
        {
            TestCompletableFuture(_pluginApi.CreateCheckoutUrl(TestPackageId, TestUsername, checkout =>
            {
                Assert.IsNotNull(checkout);
                Assert.IsFalse(string.IsNullOrEmpty(checkout.Url));
                Assert.IsFalse(string.IsNullOrEmpty(checkout.Expires));
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetAllGiftCards()
        {
            TestCompletableFuture(_pluginApi.GetAllGiftCards(giftCards =>
            {
                Assert.IsNotNull(giftCards);
                foreach (var card in giftCards.Data)
                {
                    TestGiftCard(card);
                }
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        private void TestGiftCard(GiftCard card)
        {
            Assert.IsTrue(card.Id > 0, "Gift card ID should not be negative.");
            Assert.IsTrue(card.Code.Length > 0, "Gift card code should not be empty.");
            Assert.IsNotNull(card.Balance, "Gift card balance should not be null.");
            Assert.IsTrue(card.Balance.Starting > -1, "Gift card starting balance should not be negative.");
            Assert.IsTrue(card.Balance.Remaining > -1, "Gift card remaining balance should not be negative.");
            Assert.IsNotNull(card.CreatedAt, "Gift card creation date should not be empty.");
            if (card.ExpiresAt != null)
            {
                Assert.IsNotNull(card.ExpiresAt, "Gift card expiration date should not be empty if provided.");
            }
        }
        
        [TestMethod]
        public void TestGetGiftCard()
        {
            TestCompletableFuture(_pluginApi.GetGiftCard(TestGiftCardId, giftCard =>
            {
                Assert.IsNotNull(giftCard);
                TestGiftCard(giftCard.Data);
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        // Plus plan required
        // [TestMethod]
        // public void TestCreateGiftCard()
        // {
        //     TestCompletableFuture(_pluginApi.CreateGiftCard(DateTime.Today.AddDays(7), "Testing", 127, giftCard =>
        //     {
        //         TestGiftCard(giftCard);
        //         completion.SetResult(true);
        //     }, _defaultTestApiError, _defaultTestServerError));
        // }

        [TestMethod]
        public void TestVoidGiftCard()
        {
            TestCompletableFuture(_pluginApi.VoidGiftCard(TestGiftCardId, response =>
            {
                Assert.IsNotNull(response);
                TestGiftCard(response.Data);
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));;
        }

        // Plus plan required, create a card then void it
        // [TestMethod]
        // public void TestTopUpGiftCard()
        // {
        //     TestCompletableFuture(_pluginApi.TopUpGiftCard(TestGiftCardId, 25, response =>
        //     {
        //         Assert.IsNotNull(response);
        //         TestGiftCard(response.Data);
        //         completion.SetResult(true);
        //     }, _defaultTestApiError, _defaultTestServerError));
        // }

        private void TestCoupon(Coupon coupon)
        {
            Assert.IsNotNull(coupon);
            Assert.IsTrue(coupon.Id > 0);
            Assert.IsTrue(coupon.Code.Length > 0);
            Assert.IsNotNull(coupon.Effective);
            Assert.IsNotNull(coupon.Discount);
            Assert.IsNotNull(coupon.Expire);
            Assert.IsTrue(coupon.BasketType.Length > 0);
            Assert.IsNotNull(coupon.StartDate);
            Assert.IsTrue(coupon.UserLimit >= 0);
            Assert.IsTrue(coupon.Minimum >= 0);
            Assert.IsNotNull(coupon.Username);
            Assert.IsNotNull(coupon.Note);
        }
        
        [TestMethod]
        public void TestGetAllCoupons()
        {
            TestCompletableFuture(_pluginApi.GetAllCoupons(coupons =>
            {
                Assert.IsNotNull(coupons);
                Assert.IsNotNull(coupons.Pagination);
                Assert.IsTrue(coupons.Pagination.CurrentPage >= 0);
                Assert.IsTrue(coupons.Pagination.LastPage >= 0);
                Assert.IsTrue(coupons.Pagination.TotalResults > -1);
                Assert.IsNotNull(coupons.Data);
                foreach (var coupon in coupons.Data)
                {
                    TestCoupon(coupon);
                }
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetCouponById()
        {
            TestCompletableFuture(_pluginApi.GetCouponById(TestCouponCode, coupon =>
            {
                Assert.IsNotNull(coupon.Data);
                TestCoupon(coupon.Data);
                completion.SetResult(true);
                ;
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetAllBans()
        {
            TestCompletableFuture(_pluginApi.GetAllBans(bans =>
            {
                Assert.IsNotNull(bans);
                foreach (var ban in bans.Data)
                {
                    Assert.IsTrue(ban.Id > 0, "Ban ID should not be negative.");
                    Assert.IsTrue(ban.Ip.Length > 0, "Ban IP should not be empty.");
                    Assert.IsNotNull(ban.User);
                    Assert.IsTrue(ban.User.Ign.Length > 0, "Ban username should not be empty.");
                    Assert.IsTrue(ban.User.Uuid.Length > 0, "Ban UUID should not be empty.");
                    Assert.IsTrue(ban.Reason.Length > 0, "Ban reason should not be empty.");
                    Assert.IsNotNull(ban.Time);
                }
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestCreateBan()
        {
            TestCompletableFuture(_pluginApi.CreateBan("Test Ban", "127.0.0.1", TestUsername, ban =>
            {
                Assert.IsNotNull(ban);
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        // Tebex Plus required
        // [TestMethod]
        // public void TestGetAllSales()
        // {
        //     TestCompletableFuture(_pluginApi.GetAllSales(sales =>
        //     {
        //         Assert.IsNotNull(sales);
        //         foreach (var sale in sales.Data)
        //         {
        //             Assert.IsTrue(sale.Id > 0, "Sale ID should not be negative.");
        //             Assert.IsTrue(sale.Name.Length > 0, "Sale name should not be empty.");
        //             Assert.IsNotNull(sale.Discount);
        //             Assert.IsNotNull(sale.Effective);
        //             Assert.IsTrue(sale.Expire >= -1);
        //             Assert.IsTrue(sale.Order >= -1);
        //             Assert.IsTrue(sale.Start >= -1);
        //         }
        //         completion.SetResult(true);
        //     }, _defaultTestApiError, _defaultTestServerError));
        // }

        // Tebex Plus required
        // [TestMethod]
        // public void TestGetUser()
        // {
        //     TestCompletableFuture(_pluginApi.GetUser(TestUsername, user =>
        //     {
        //         Assert.IsNotNull(user);
        //         completion.SetResult(true);
        //     }, _defaultTestApiError, _defaultTestServerError));
        // }

        [TestMethod]
        public void TestGetActivePackagesForCustomer()
        {
            TestCompletableFuture(_pluginApi.GetActivePackagesForCustomer(TestPlayerUuid, packages =>
            {
                Assert.IsNotNull(packages);
                foreach (var activePackage in packages)
                {
                    Assert.IsNotNull(activePackage.Package);
                    Assert.IsNotNull(activePackage.Package.Id);
                    Assert.IsNotNull(activePackage.Package.Name);
                    Assert.IsTrue(activePackage.TxnId.Length > 0);
                    Assert.IsTrue(activePackage.Quantity >= 0);
                    Assert.IsNotNull(activePackage.Date);
                }
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetActivePackageById()
        {
            TestCompletableFuture(_pluginApi.GetActivePackageById(TestPlayerUuid, TestPackageId, package =>
            {
                Assert.IsNotNull(package);
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }
    }
}