using System;
using NUnit.Framework;
using SalesforceSharp.Security;
using SalesforceSharp.FunctionalTests.Stubs;
using TestSharp;

namespace SalesforceSharp.FunctionalTests
{
    [TestFixture]
    public class SalesforceClientTest
    {
        #region Authenticate
        [Test]
        public void Authenticate_InvalidUsername_AuthenticationFailure()
        {
            var target = new SalesforceClient();

            ExceptionAssert.IsThrowing(new SalesforceException(SalesforceError.AuthenticationFailure, "authentication failure"), () =>
            {
                target.Authenticate(CreateAuthenticationFlow(TestConfig.ClientId, TestConfig.ClientSecret, "invalid user name", TestConfig.Password));
            });

            Assert.IsFalse(target.IsAuthenticated);
        }

        [Test]
        public void Authenticate_InvalidPassword_InvalidPassword()
        {
            var target = new SalesforceClient();

            ExceptionAssert.IsThrowing(new SalesforceException(SalesforceError.InvalidPassword, "authentication failure - invalid password"), () =>
            {
                target.Authenticate(CreateAuthenticationFlow(TestConfig.ClientId, TestConfig.ClientSecret, TestConfig.Username, "invalid password"));
            });

            Assert.IsFalse(target.IsAuthenticated);
        }

        [Test]
        public void Authenticate_InvalidClientId_InvalidClientId()
        {
            var target = new SalesforceClient();

            ExceptionAssert.IsThrowing(new SalesforceException(SalesforceError.InvalidClient, "client identifier invalid"), () =>
            {
                target.Authenticate(CreateAuthenticationFlow("Invalid client id", TestConfig.ClientSecret, "invalid user name", TestConfig.Password));
            });

            Assert.IsFalse(target.IsAuthenticated);
        }

        [Test]
        public void Authenticate_InvalidClientSecret_InvalidClientSecret()
        {
            var target = new SalesforceClient();

            ExceptionAssert.IsThrowing(new SalesforceException(SalesforceError.InvalidClient, "invalid client credentials"), () =>
            {
                target.Authenticate(CreateAuthenticationFlow(TestConfig.ClientId, "invalid client secret", "invalid user name", TestConfig.Password));
            });

            Assert.IsFalse(target.IsAuthenticated);
        }

        [Test]
        public void Authenticate_ValidCredentials_Authenticated()
        {
            var target = new SalesforceClient();
            target.Authenticate(CreateAuthenticationFlow(TestConfig.ClientId, TestConfig.ClientSecret, TestConfig.Username, TestConfig.Password));
            Assert.IsTrue(target.IsAuthenticated);
        }
        #endregion

        #region Query
        [Test]
        public void Query_InvalidQuery_Exception()
        {
            var target = CreateClientAndAuth();

            ExceptionAssert.IsThrowing(typeof(SalesforceException), () =>
            {
                target.Query<RecordStub>("SELECT id, name, FROM " + TestConfig.ObjectName);
            });
        }

        [Test]
        public void Query_ValidQueryWithObject_Result()
        {
            var target = CreateClientAndAuth();
            var actual = target.Query<RecordStub>("SELECT id, name FROM Account");
            Assert.IsNotNull(actual);

            if (actual.Count > 0)
            {
                Assert.IsNotNullOrEmpty(actual[0].Id);
                Assert.IsNotNullOrEmpty(actual[0].Name);
            }

            actual = target.Query<RecordStub>("SELECT id, name FROM Account WHERE LastModifiedDate = 2013-12-01T12:00:00+00:00");
            Assert.IsNotNull(actual);
        }


        /// <summary>
        /// To validate this issue: https://github.com/giacomelli/SalesforceSharp/issues/4.
        /// </summary>
        [Test]
        public void Query_ValidQueryWithSpecialChars_Result()
        {
            var target = CreateClientAndAuth();
            var actual = target.Query<RecordStub>("SELECT id, name, description FROM Account WHERE LastModifiedDate >= 2013-12-01T12:00:00+00:00");
            Assert.IsNotNull(actual);
        }

		/// <summary>
		/// To validate this issue: https://github.com/giacomelli/SalesforceSharp/issues/6.
		/// </summary>
		[Test]
		public void Query_ValidQueryClassWithFields_ResultNoFieldsBind()
		{
			var target = CreateClientAndAuth();

			// Public FIELDS are not supported.
			var actual1 = target.Query<ContactStubWithFields>("SELECT Id, Name, Email FROM Contact LIMIT 1 OFFSET 0");
			Assert.AreEqual(1, actual1.Count);

			var first1 = actual1 [0];
			TextAssert.IsNullOrEmpty (first1.Id);
			TextAssert.IsNullOrEmpty (first1.Name);
		
			// Public PROPERTIES are supported.
			var actual2 = target.Query<ContactStub>("SELECT Id, Name, Email FROM Contact LIMIT 1 OFFSET 0");
			Assert.AreEqual(1, actual2.Count);

			var first2 = actual2 [0];
			TextAssert.IsNotNullOrEmpty (first2.Id);
			TextAssert.IsNotNullOrEmpty (first2.Name);
		}

        [Test]
        public void Query_ValidQueryWithObjectWrongPropertyTypes_Exception()
        {
            var target = CreateClientAndAuth();

            ExceptionAssert.IsThrowing(typeof(FormatException), () =>
            {
                target.Query<WrongRecordStub>("SELECT IsDeleted FROM Account");
            });

        }
        #endregion

        #region
        [Test]
        public void QueryActionBatch_ValidQuery_AllRecords()
        {
            var target = CreateClientAndAuth();
            var queryString = "SELECT id, name, description ";
            queryString += " FROM Account";

            var totalRecords = 0;

            var actual = target.QueryActionBatch<RecordStub>(queryString, s =>
            {
                totalRecords += s.Count;
            });

            Assert.IsNotNull(totalRecords);
            Assert.AreNotEqual(0, totalRecords);
			Assert.AreEqual (totalRecords, actual.Count);
        }

        #endregion

        #region FindById
        [Test]
        public void FindById_NotExistingID_Null()
        {
            var target = CreateClientAndAuth();
            Assert.IsNull(target.FindById<RecordStub>("Contact", "003i000000K2BP0AAM"));
        }

        [Test]
        public void FindById_ValidId_Record()
        {
            var target = CreateClientAndAuth();
            var record = new
            {
                FirstName = "Name " + DateTime.Now.Ticks,
                LastName = "Last name"
            };

            var id = target.Create("Contact", record);
            var actual = target.FindById<ContactStub>("Contact", id);

            Assert.IsNotNull(actual);
            Assert.AreEqual(record.FirstName, actual.FirstName);
            Assert.AreEqual(record.LastName, actual.LastName);
        }
        #endregion

        #region ReadMetaData
        [Test]
        public void ReadMetaData_ValidObjectName_Metadata()
        {
            var target = CreateClientAndAuth();

            string result = target.ReadMetaData("Account");

            Assert.IsNotNullOrEmpty(result);
        }
        #endregion

        #region Create
        [Test]
        public void Create_ValidRecordWithAnonymous_Created()
        {
            var target = CreateClientAndAuth();
            var record = new
            {
                FirstName = "Name " + DateTime.Now.Ticks,
                LastName = "Last name"
            };

            var id = target.Create("Contact", record);
            Assert.IsFalse(String.IsNullOrWhiteSpace(id));
        }

        [Test]
        public void Create_ValidRecordWithClassWithWrongProperties_Exception()
        {
            var target = CreateClientAndAuth();
            var record = new
            {
                FirstName1 = "Name " + DateTime.Now.Ticks,
                LastName = "Last name"
            };

            ExceptionAssert.IsThrowing(new SalesforceException(SalesforceError.InvalidField, "No such column 'FirstName1' on sobject of type Contact"), () =>
            {
                target.Create("Contact", record);
            });
        }
        #endregion

        #region Update
        [Test]
        public void Update_InvalidId_Exception()
        {
            var target = CreateClientAndAuth();

            ExceptionAssert.IsThrowing(new SalesforceException(SalesforceError.NotFound, "Provided external ID field does not exist or is not accessible: INVALID ID"), () =>
            {
                target.Update(TestConfig.ObjectName, "INVALID ID", new { Name = "TEST" });
            });
        }

        [Test]
        public void Update_ValidRecordWithAnonymous_Updated()
        {
            var target = CreateClientAndAuth();
            var actual = target.Query<RecordStub>("SELECT id, name, description FROM Account");
            Assert.IsNotNull(actual);

            if (actual.Count > 0)
            {
                Assert.IsTrue(target.Update("Account", actual[0].Id, new { Description = DateTime.Now + " UPDATED" }));
            }
        }

        [Test]
        public void Update_ValidRecordWithClass_Updated()
        {
            var target = CreateClientAndAuth();
            var actual = target.Query<RecordStub>("SELECT id, name, description FROM Account");
            Assert.IsNotNull(actual);

            if (actual.Count > 0)
            {
                Assert.IsTrue(target.Update("Account", actual[0].Id, new RecordStub { Name = actual[0].Name, Description = DateTime.Now + " UPDATED" }));
            }
        }

        [Test]
        public void Update_ValidRecordWithClassWithWrongProperties_Exception()
        {
            var target = CreateClientAndAuth();
            var actual = target.Query<RecordStub>("SELECT id, name, description FROM Account");
            Assert.IsNotNull(actual);

            if (actual.Count > 0)
            {
                ExceptionAssert.IsThrowing(new SalesforceException(SalesforceError.InvalidFieldForInsertUpdate, "Unable to create/update fields: IsDeleted. Please check the security settings of this field and verify that it is read/write for your profile or permission set."), () =>
                {
                    target.Update("Account", actual[0].Id, new WrongRecordStub { Name = actual[0].Name, Description = DateTime.Now + " UPDATED" });
                });
            }
        }
        #endregion

        #region Delete
        [Test]
        public void Delete_MalFormedId_Exception()
        {
            var target = CreateClientAndAuth();

            ExceptionAssert.IsThrowing(new SalesforceException(SalesforceError.EntityIsDeleted, "malformed id 003i000000K27rxAAC"), () =>
            {
                target.Delete("Contact", "003i000000K27rxAAC");
            });
        }

        [Test]
        public void Delete_AlreadyDeleted_Exception()
        {
            var target = CreateClientAndAuth();
            var record = new
            {
                FirstName = "Name " + DateTime.Now.Ticks,
                LastName = "Last name"
            };

            var id = target.Create("Contact", record);

            Assert.IsTrue(target.Delete("Contact", id));

            ExceptionAssert.IsThrowing(new SalesforceException(SalesforceError.EntityIsDeleted, "Entity is deleted"), () =>
            {
                target.Delete("Contact", id);
            });
        }

        [Test]
        public void Delete_ExistingId_Deleted()
        {
            var target = CreateClientAndAuth();
            var record = new
            {
                FirstName = "Name " + DateTime.Now.Ticks,
                LastName = "Last name"
            };

            var id = target.Create("Contact", record);

            Assert.IsTrue(target.Delete("Contact", id));
        }
        #endregion

        #region Helpers
        private SalesforceClient CreateClientAndAuth()
        {
            return CreateClientAndAuth(TestConfig.ClientId, TestConfig.ClientSecret, TestConfig.Username, TestConfig.Password);
        }

        private UsernamePasswordAuthenticationFlow CreateAuthenticationFlow(string clientId, string clientSecret, string username, string password)
        {
            var flow = new UsernamePasswordAuthenticationFlow(clientId, clientSecret, username, password);
            flow.TokenRequestEndpointUrl = TestConfig.TokenRequestEndpointUrl;

            return flow;
        }

        private SalesforceClient CreateClientAndAuth(
            string clientId,
            string clientSecret,
            string username,
            string password)
        {
            var client = new SalesforceClient();
            var authenticationFlow = CreateAuthenticationFlow(clientId, clientSecret, username, password);

            client.Authenticate(authenticationFlow);

            return client;
        }
        #endregion
    }
}