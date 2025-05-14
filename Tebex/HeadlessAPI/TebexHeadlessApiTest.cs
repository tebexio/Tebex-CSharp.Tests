using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tebex.Common;
using Tebex.HeadlessAPI;

namespace Tebex_Unity.Tests.Tebex.HeadlessAPI
{
    [TestClass]
    [TestSubject(typeof(TebexHeadlessApi))]
    public class TebexHeadlessApiTest
    {
        TebexHeadlessApi headless;
        private static TaskCompletionSource<bool> completion;
        private static string TestPublicToken = "t66x-7cd928b1e9399909e6810edac6dc1fd1eefc57cb";
        private static string TestPrivateKey = "your-private-key";
        private static string TestGiftCardCode = "1234567890";
        private static string TestCreatorCode = "TebexDev";
        private static string TestUsername = "TebexDev";
        private static long TestUsernameId = 76561198042467022;
        private static string TestEmail = "tebex-integrations@overwolf.com";
        private static string TestCoupon = "Academy10";
        private static int TestUpgradeTeirPackageId = 6834822;
        private static int TestTierId = 40796;
        
        Action<HeadlessApiError> _defaultTestApiError = apiError =>
        {
            try
            {
                Assert.Fail("API error: " + apiError.AsException());
            }
            catch (Exception ex)
            {
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
            headless = TebexHeadlessApi.Initialize(new SystemHeadlessAdapter(), TestPublicToken);
            headless.SetPrivateKey(TestPrivateKey);
            completion = new TaskCompletionSource<bool>();
        }
        
        [TestMethod]
        public void TestGetAllPackages()
        {
            TestCompletableFuture(headless.GetAllPackages(packages =>
            {
                try
                {
                    Assert.IsTrue(packages.Data.Count > 0, "The packages list is not empty.");
                    foreach (var package in packages.Data)
                    {
                        TestPackage(package);
                    }

                    completion.SetResult(true);
                }
                catch (AssertFailedException ex)
                {
                    completion.SetException(ex);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetAllCategories()
        {
            TestCompletableFuture(headless.GetAllCategories(categories =>
            {
                try
                {
                    Assert.IsTrue(categories.Data.Count > 0, "The categories list is not empty.");
                    foreach (var category in categories.Data)
                    {
                        TestCategory(category);
                    }
                    completion.SetResult(true);
                }
                catch (AssertFailedException ex)
                {
                    completion.SetException(ex);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestCreateAndGetBasket()
        {
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload, basket =>
            {
                try
                {
                    TestBasket(basket.Data);
                    headless.GetBasket(basket.Data.Ident, basket2 =>
                    {
                        TestBasket(basket2.Data);
                        completion.SetResult(true);
                    }, _defaultTestApiError, _defaultTestServerError);
                }
                catch (AssertFailedException ex)
                {
                    completion.SetException(ex);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetPackage()
        {
            TestCompletableFuture(headless.GetAllPackages(packages =>
            {
                headless.GetPackage(packages.Data[0].Id, package2 =>
                {
                    TestPackage(package2.Data);
                    completion.SetResult(true);
                }, _defaultTestApiError, _defaultTestServerError);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetPackageWithBasketIdent()
        {
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload, basket =>
            {
                try
                {
                    TestBasket(basket.Data);
                    headless.GetAllPackages(packages =>
                    {
                        headless.GetPackage(packages.Data[0].Id, basket.Data.Ident, package2 =>
                        {
                            TestPackage(package2.Data);
                            completion.SetResult(true);
                        }, _defaultTestApiError, _defaultTestServerError);
                    }, _defaultTestApiError, _defaultTestServerError);
                }
                catch (AssertFailedException ex)
                {
                    completion.SetException(ex);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetAllCategoriesIncludingPackages()
        {
            TestCompletableFuture(headless.GetAllCategoriesIncludingPackages(categories =>
            {
                try
                {
                    foreach (var category in categories.Data)
                    {
                        TestCategory(category);
                        foreach (var package in category.Packages)
                        {
                            TestPackage(package);
                            completion.SetResult(true);
                        }
                    }
                } 
                catch (AssertFailedException ex)
                {
                    completion.SetException(ex);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }
        [TestMethod]
        public void TestGetCategory()
        {
            TestCompletableFuture(headless.GetAllCategories(categories =>
            {
                try
                {
                    var category = categories.Data[0];
                    headless.GetCategory(category.Id, remoteCategory =>
                    {
                        TestCategory(remoteCategory.Data);
                        Assert.IsTrue(remoteCategory.Data.Packages.Count == 0,
                            "The category packages list is empty because packages were not requested.");
                        completion.SetResult(true);
                    }, _defaultTestApiError, _defaultTestServerError);
                }
                catch (AssertFailedException ex)
                {
                    completion.SetException(ex);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }
        
        [TestMethod]
        public void TestGetCategoryIncludingPackages()
        {
            TestCompletableFuture(headless.GetAllCategories(categories =>
            {
                try
                {
                    var category = categories.Data[0];
                    headless.GetCategoryIncludingPackages(category.Id, remoteCategory =>
                    {
                        TestCategory(remoteCategory.Data);
                        Assert.IsTrue(remoteCategory.Data.Packages.Count > 0,
                            "The category packages list is not empty.");
                        completion.SetResult(true);
                    }, _defaultTestApiError, _defaultTestServerError);
                }
                catch (AssertFailedException ex)
                {
                    completion.SetException(ex);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }
        
        [TestMethod]
        public void TestCreateAndAddPackage()
        {
            // This will create a basket, query all packages, take the first package and add it to the basket, then check if the basket is in a valid state.
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload, basket =>
            {
                try
                {
                    headless.GetAllPackages(packs =>
                    {
                        var package = packs.Data[0];
                        headless.AddPackageToBasket(basket.Data.Ident, new AddPackagePayload(package.Id), addedBasket =>
                        {
                            Assert.IsTrue(addedBasket.Data.Packages.Count == 1, "The basket has exactly one package.");
                            TestBasket(addedBasket.Data);
                            Assert.IsTrue(addedBasket.Data.TotalPrice > 0, "The basket total price is valid (> 0).");
                            completion.SetResult(true);
                        }, _defaultTestApiError, _defaultTestServerError);
                    }, _defaultTestApiError, _defaultTestServerError);
                } 
                catch (AssertFailedException ex)
                {
                    completion.SetException(ex);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestUpdatePackageQuantity()
        {
            // This will create a basket, query all packages, take the first package and add it to the basket, then update the quantity, and finally check if the basket is in a valid state.
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload, basket =>
            {
                try
                {
                    headless.GetAllPackages(packs =>
                    {
                        var package = packs.Data[0];
                        headless.AddPackageToBasket(basket.Data.Ident, new AddPackagePayload(package.Id), addedBasket =>
                        {
                            Assert.IsTrue(addedBasket.Data.Packages.Count == 1, "The basket has exactly one package.");
                            TestBasket(addedBasket.Data);
                            Assert.IsTrue(addedBasket.Data.TotalPrice > 0, "The basket total price is valid (> 0).");

                            headless.UpdatePackageQuantity(addedBasket.Data.Ident, package.Id, 2, updatedBasket =>
                            {
                                Assert.IsTrue(updatedBasket.Data.Packages.Count == 1, "The basket has exactly one package.");
                                Assert.IsTrue(updatedBasket.Data.Packages[0].In.Quantity == 2, "The package quantity is updated to 2.");
                                completion.SetResult(true);
                            }, _defaultTestApiError, _defaultTestServerError);
                        }, _defaultTestApiError, _defaultTestServerError);
                    }, _defaultTestApiError, _defaultTestServerError);
                }
                catch (AssertFailedException ex)
                {
                    completion.SetException(ex);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestGetBasketAuthLinks()
        {
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload, basket =>
            {
                try
                {
                    headless.GetBasketAuthLinks(basket.Data.Ident, "https://tebex.io/return/", links =>
                    {
                        foreach (var authLink in links.LinksArray)
                        {
                            Assert.IsNotNull(authLink.Url, "The auth link URL is not null.");
                            Assert.IsNotNull(authLink.Name, "The auth link name is not null.");
                        }

                        completion.SetResult(true);
                    }, _defaultTestApiError, _defaultTestServerError);
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestRemovePackageFromBasket()
        {
            // This will create a basket, query all packages, take the first package and add it to the basket, then remove it, and check if removal was successful
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload, basket =>
            {
                try
                {
                    headless.GetAllPackages(packs =>
                    {
                        var package = packs.Data[0];
                        headless.AddPackageToBasket(basket.Data.Ident, new AddPackagePayload(package.Id), addedBasket =>
                        {
                            Assert.IsTrue(addedBasket.Data.Packages.Count == 1, "The basket has exactly one package.");
                            TestBasket(addedBasket.Data);
                            Assert.IsTrue(addedBasket.Data.TotalPrice > 0, "The basket total price is valid (> 0).");

                            headless.RemovePackageFromBasket(basket.Data.Ident, package.Id, removedBasket =>
                            {
                                TestBasket(removedBasket.Data);
                                Assert.IsTrue(removedBasket.Data.Packages.Count == 0, "The basket has no packages.");
                                completion.SetResult(true);
                            }, _defaultTestApiError, _defaultTestServerError);
                        }, _defaultTestApiError, _defaultTestServerError);
                    }, _defaultTestApiError, _defaultTestServerError);
                } 
                catch (AssertFailedException ex)
                {
                    completion.SetException(ex);
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestApplyCreatorCode()
        {
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload,
                basket => {
                    headless.ApplyCreatorCode(basket.Data.Ident, TestCreatorCode,
                        empty => { completion.SetResult(true); }, _defaultTestApiError, _defaultTestServerError);
                }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestRemoveCreatorCode()
        {
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload, basket =>
            {
                headless.ApplyCreatorCode(basket.Data.Ident, TestCreatorCode, couponBasket =>
                {
                    headless.RemoveCreatorCode(basket.Data.Ident, response =>
                    {
                        completion.SetResult(true);    
                    }, _defaultTestApiError, _defaultTestServerError);
                }, _defaultTestApiError, _defaultTestServerError);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestApplyGiftCard()
        {
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload, basket =>
            {
                headless.ApplyGiftCard(basket.Data.Ident, TestGiftCardCode, couponBasket =>
                {
                    completion.SetResult(true);
                }, _defaultTestApiError, _defaultTestServerError);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestRemoveGiftCard()
        {
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload, basket =>
            {
                headless.ApplyGiftCard(basket.Data.Ident, TestGiftCardCode, couponBasket =>
                {
                    headless.RemoveGiftCard(basket.Data.Ident, TestGiftCardCode,response =>
                    {
                        completion.SetResult(true);    
                    }, _defaultTestApiError, _defaultTestServerError);
                }, _defaultTestApiError, _defaultTestServerError);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestApplyCoupon()
        {
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload, basket =>
            {
                headless.GetAllPackages(packages =>
                {
                    headless.AddPackageToBasket(basket.Data.Ident, new AddPackagePayload(packages.Data[0].Id), packageBasket =>
                        {
                            headless.ApplyCoupon(basket.Data.Ident, TestCoupon, couponBasket =>
                            {
                                completion.SetResult(true);    
                            }, _defaultTestApiError, _defaultTestServerError);
                        }, _defaultTestApiError, _defaultTestServerError);
                }, _defaultTestApiError, _defaultTestServerError);
            }, _defaultTestApiError, _defaultTestServerError));
        }

        [TestMethod]
        public void TestRemoveCoupon()
        {
            var payload = new CreateBasketPayload(TestEmail, "", "https://tebex.io/cancel", "https://tebex.io/complete");
            TestCompletableFuture(headless.CreateBasket(payload, basket =>
            {
                headless.GetAllPackages(packages =>
                {
                    headless.AddPackageToBasket(basket.Data.Ident, new AddPackagePayload(packages.Data[0].Id), packageBasket =>
                    {
                        headless.ApplyCoupon(basket.Data.Ident, TestCoupon, couponBasket =>
                        {
                            headless.RemoveCoupon(basket.Data.Ident, TestCoupon,response =>
                            {
                                completion.SetResult(true);    
                            }, _defaultTestApiError, _defaultTestServerError);
                        }, _defaultTestApiError, _defaultTestServerError);
                    }, _defaultTestApiError, _defaultTestServerError);
                }, _defaultTestApiError, _defaultTestServerError);
            }, _defaultTestApiError, _defaultTestServerError));
        }
        
        [TestMethod]
        public void TestGetTieredCategoriesForUser()
        {
            TestCompletableFuture(headless.GetTieredCategoriesForUser(TestUsernameId, categories =>
            {
                var foundTier = false;
                foreach (var category in categories.Data)
                {
                    if (category.ActiveTier != null)
                    {
                        foundTier = true;
                        // Test tiered category and active tier 
                        // Assert.IsTrue(category.Tiered, "The category is tiered."); TODO
                        Assert.IsTrue(category.ActiveTier.Id > 0);
                        Assert.IsTrue(category.ActiveTier.Id > 0, "The active tier ID is valid (> 0).");
                        Assert.IsTrue(category.ActiveTier.Active);
                        TestPackage(category.ActiveTier.Package);
                        Assert.IsTrue(category.ActiveTier.CreatedAt.Length > 0);
                        Assert.IsTrue(category.ActiveTier.UsernameId > 0);
                        Assert.IsTrue(category.ActiveTier.NextPaymentDate.Length > 0);
                        Assert.IsTrue(category.ActiveTier.Status != null);
                        Assert.IsTrue(category.ActiveTier.RecurringPaymentReference.Length > 0);
                        completion.SetResult(true);
                    }
                }

                if (!foundTier)
                {
                    completion.SetException(new Exception("No tiers found for the user."));
                }
            }, _defaultTestApiError, _defaultTestServerError));
        }
        
        [TestMethod]
        public void TestUpdateTier()
        {
            TestCompletableFuture(headless.UpdateTier(TestTierId, TestUpgradeTeirPackageId, categories =>
            {
                completion.SetResult(true);
            }, _defaultTestApiError, _defaultTestServerError));
        }
        
        private void TestBasket(Basket basket)
        {
            Assert.IsNotNull(basket, "The basket is not null.");
            //Assert.IsTrue(basket.Id > 0, "The basket ID is assigned.");
            Assert.IsTrue(basket.Ident.Length > 0, "The basket ident is assigned.");
            Assert.IsNotNull(basket.Packages, "The basket items are not null.");
            Assert.IsTrue(basket.Packages.Count >= 0, "The basket items count is valid (>= 0).");
            Assert.IsTrue(basket.Ip.Length > 0, "The IP address is valid.");
            Assert.IsTrue(basket.Currency.Length > 0, "The currency is valid.");
            Assert.IsTrue(basket.Country.Length > 0, "The country is valid.");
            
            foreach (var item in basket.Packages)
            {
                Assert.IsNotNull(item, "Basket item is not null.");
                Assert.IsTrue(item.Id > 0, "Item ID is valid.");
                Assert.IsTrue(item.Name.Length > 0, "Item name is not empty.");
                Assert.IsTrue(item.In.Price >= 0, "Item price is valid (>= 0).");
                Assert.IsNotNull(item.Description, "Item description is not null.");
            }

            Assert.IsTrue(basket.BasePrice >= 0, "Base price is valid (>= 0).");
            Assert.IsTrue(basket.TotalPrice >= 0, "Total is valid (>= 0).");
        }

        private void TestPackage(Package package)
        {
            Assert.IsTrue(package.Id > 0, "The ID should be greater than 0.");
            Assert.IsNotNull(package.Name, "The Name property should not be null.");
            Assert.IsTrue(package.Name.Length > 0, "The Name property should not be empty.");
            Assert.IsNotNull(package.Description, "The Description property should not be null.");
            Assert.IsTrue(package.Description.Length > 0, "The Description property should not be empty.");
            Assert.IsNotNull(package.Type, "The Type property should not be null.");
            Assert.IsTrue(package.Type.Length > 0, "The Type property should not be empty.");
            Assert.IsNotNull(package.Category, "The Category property should not be null.");
            Assert.IsTrue(package.BasePrice >= 0, "The BasePrice property should be greater than or equal to 0.");
            Assert.IsTrue(package.SalesTax >= 0, "The SalesTax property should be greater than or equal to 0.");
            Assert.IsTrue(package.TotalPrice >= 0, "The TotalPrice property should be greater than or equal to 0.");
            Assert.IsTrue(package.TotalPrice >= package.BasePrice,
                "The TotalPrice should be greater than or equal to the BasePrice.");
            Assert.IsNotNull(package.Currency, "The Currency property should not be null.");
            Assert.IsTrue(package.Currency.Length > 0, "The Currency property should not be empty.");
            Assert.IsTrue(package.Discount >= 0, "The Discount property should be greater than or equal to 0.");
            Assert.IsNotNull(package.DisableQuantity, "The DisableQuantity property should not be null.");
            Assert.IsNotNull(package.DisableGifting, "The DisableGifting property should not be null.");
            Assert.IsNotNull(package.CreatedAt, "The CreatedAt property should not be null.");
            Assert.IsTrue(DateTime.TryParse(package.CreatedAt, out _),
                "The CreatedAt property should contain a valid date.");
            Assert.IsNotNull(package.UpdatedAt, "The UpdatedAt property should not be null.");
            Assert.IsTrue(DateTime.TryParse(package.UpdatedAt, out _),
                "The UpdatedAt property should contain a valid date.");
            Assert.IsTrue(package.Order >= 0, "The Order property should be greater than or equal to 0.");
        }

        public void TestCategory(Category category)
        {
            Assert.IsTrue(category.Id >= 0, "The Id property should be greater than or equal to 0.");
            Assert.IsNotNull(category.Name, "The Name property should not be null.");
            Assert.IsTrue(category.Name.Length > 0, "The Name property should not be empty.");
            //Assert.IsNotNull(category.Slug, "The Slug property should not be null.");
            //Assert.IsTrue(category.Slug.Length > 0, "The Slug property should not be empty.");
            Assert.IsNotNull(category.Description, "The Description property should not be null.");
            Assert.IsTrue(category.Order >= 0, "The Order property should be greater than or equal to 0.");
            Assert.IsNotNull(category.DisplayType, "The DisplayType property should not be null.");
            Assert.IsTrue(category.DisplayType.Length > 0, "The DisplayType property should not be empty.");
            Assert.IsNotNull(category.Packages, "The Packages property should not be null.");
            Assert.IsTrue(category.Packages.Count >= 0, "The Packages property should have a valid count (>= 0).");
            foreach (var package in category.Packages)
            {
                Assert.IsNotNull(package, "Each package item in the Packages list should not be null.");
                Assert.IsTrue(package.Id > 0, "Each package Id should be greater than 0.");
                Assert.IsNotNull(package.Name, "Each package Name property should not be null.");
                Assert.IsTrue(package.Name.Length > 0, "Each package Name property should not be empty.");
            }
        }
    }
}