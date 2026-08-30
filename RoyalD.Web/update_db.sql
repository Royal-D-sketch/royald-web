ALTER TABLE OutstandingDebts ADD COLUMN BadDebtAmount decimal(18,2) NULL;
ALTER TABLE OutstandingDebts ADD COLUMN BadDebtDate TEXT NULL;
ALTER TABLE OutstandingDebts ADD COLUMN DeliveringDate TEXT NULL;
ALTER TABLE OutstandingDebts ADD COLUMN IsLocked INTEGER NOT NULL DEFAULT 0;
ALTER TABLE OutstandingDebts ADD COLUMN IsReturnCutFromBill INTEGER NOT NULL DEFAULT 0;
ALTER TABLE OutstandingDebts ADD COLUMN PostponedDate TEXT NULL;
ALTER TABLE OutstandingDebts ADD COLUMN ReturnAmount decimal(18,2) NULL;
ALTER TABLE OutstandingDebts ADD COLUMN WaitingGoodsDate TEXT NULL;
