       IDENTIFICATION DIVISION.                                                 
                                                                                
       PROGRAM-ID.  SRT1000.
                                                                                
       ENVIRONMENT DIVISION.                                                    
                                                                                
       INPUT-OUTPUT SECTION.                                                    
                                                                                
       FILE-CONTROL.                                                            

           SELECT RCTTRAN  ASSIGN TO RCTTRAN.
           SELECT OLDMAST  ASSIGN TO OLDMAST.
           SELECT NEWMAST  ASSIGN TO NEWMAST
                           FILE STATUS IS NEWMAST-FILE-STATUS.
           SELECT ERRTRAN  ASSIGN TO ERRTRAN
                           FILE STATUS IS ERRTRAN-FILE-STATUS.
           SELECT SORTWORK ASSIGN TO SORTWK01.
                                                                                
       DATA DIVISION.                                                           
                                                                                
       FILE SECTION.                                                            
                                                                                
       FD  RCTTRAN.
                                                                                
       01  TRANSACTION-RECORD      PIC X(23).
                                                                                
       FD  OLDMAST.
                                                                                
       01  OLD-MASTER-RECORD       PIC X(70).
                                                                                
       FD  NEWMAST.
                                                                                
       01  NEW-MASTER-RECORD       PIC X(70).
                                                                                
       FD  ERRTRAN.
                                                                                
       01  ERROR-TRANSACTION       PIC X(23).

       SD  SORTWORK.

       01  SORT-WORK-AREA.
           05  SW-ITEM-NO          PIC X(5).
           05  FILLER              PIC X(18).

       WORKING-STORAGE SECTION.                                                 
                                                                                
       01  SWITCHES.
           05  TRANSACTION-EOF-SWITCH          PIC X   VALUE "N".
               88  TRANSACTION-EOF                     VALUE "Y".
           05  VALID-TRANSACTION-SWITCH        PIC X   VALUE "Y".
               88  VALID-TRANSACTION                   VALUE "Y".
           05  FIRST-EXECUTION-SWITCH          PIC X   VALUE "Y".
               88  FIRST-EXECUTION                     VALUE "Y".
           05  ALL-RECORDS-PROCESSED-SWITCH    PIC X   VALUE "N".
               88  ALL-RECORDS-PROCESSED               VALUE "Y".

       01  FILE-STATUS-FIELDS.
           05  NEWMAST-FILE-STATUS     PIC XX.
               88  NEWMAST-SUCCESSFUL          VALUE "00".
           05  ERRTRAN-FILE-STATUS     PIC XX.
               88  ERRTRAN-SUCCESSFUL          VALUE "00".

       01  RECEIPT-TRANSACTION.
           05  RT-ITEM-NO              PIC X(5).
           05  RT-VENDOR-NO            PIC X(5).
           05  RT-RECEIPT-DATE         PIC X(8).
           05  RT-RECEIPT-QUANTITY-X   PIC X(5).
           05  RT-RECEIPT-QUANTITY REDEFINES RT-RECEIPT-QUANTITY-X
                                       PIC 9(5).
                                                                                
       01  INVENTORY-MASTER-RECORD.
           05  IM-ITEM-NO              PIC X(5).
           05  IM-DESCRIPTIVE-DATA.
               10  IM-ITEM-DESC        PIC X(40).
               10  IM-UNIT-COST        PIC S9(3)V99.
               10  IM-UNIT-PRICE       PIC S9(3)V99.
           05  IM-INVENTORY-DATA.
               10  IM-REORDER-POINT    PIC S9(5).
               10  IM-ON-HAND          PIC S9(5).
               10  IM-ON-ORDER         PIC S9(5).

       PROCEDURE DIVISION.                                                      
                                                                                
       000-UPDATE-INVENTORY-FILE.
                                                                                
           OPEN INPUT  OLDMAST
                       RCTTRAN
                OUTPUT NEWMAST
                       ERRTRAN.
           SORT SORTWORK
               ON ASCENDING KEY SW-ITEM-NO
               INPUT PROCEDURE IS 100-EDIT-RECEIPT-TRANSACTIONS
               OUTPUT PROCEDURE IS 400-UPDATE-INVENTORY-RECORDS.
           CLOSE RCTTRAN
                 OLDMAST
                 NEWMAST
                 ERRTRAN.
           STOP RUN.                                                            

       100-EDIT-RECEIPT-TRANSACTIONS.

           PERFORM 200-EDIT-RECEIPT-TRAN
               UNTIL TRANSACTION-EOF.

       200-EDIT-RECEIPT-TRAN.

           PERFORM 210-READ-RECEIPT-TRANSACTION.
           IF NOT TRANSACTION-EOF
               PERFORM 220-EDIT-TRANSACTION-FIELDS
               IF VALID-TRANSACTION
                   PERFORM 230-RELEASE-VALID-TRANSACTION
               ELSE
                   PERFORM 240-WRITE-ERROR-TRANSACTION.

       210-READ-RECEIPT-TRANSACTION.

           READ RCTTRAN INTO RECEIPT-TRANSACTION
               AT END
                   SET TRANSACTION-EOF TO TRUE.

       220-EDIT-TRANSACTION-FIELDS.

           SET VALID-TRANSACTION TO TRUE.
           IF       RT-ITEM-NO NOT NUMERIC
                 OR RT-VENDOR-NO NOT NUMERIC
                 OR RT-RECEIPT-QUANTITY-X NOT NUMERIC
               MOVE "N" TO VALID-TRANSACTION-SWITCH.

       230-RELEASE-VALID-TRANSACTION.

           RELEASE SORT-WORK-AREA FROM RECEIPT-TRANSACTION.

       240-WRITE-ERROR-TRANSACTION.

           WRITE ERROR-TRANSACTION FROM RECEIPT-TRANSACTION.
           IF NOT ERRTRAN-SUCCESSFUL
               DISPLAY "WRITE ERROR ON ERRTRAN FOR ITEM NUMBER "
                   RT-ITEM-NO
               DISPLAY "FILE STATUS CODE IS " ERRTRAN-FILE-STATUS.

      *300-SORT-VALID-TRANSACTIONS.  DUMMY MODULE DONE BY SORT PROGRAM.

       400-UPDATE-INVENTORY-RECORDS.

           MOVE LOW-VALUE TO IM-ITEM-NO.
           PERFORM 500-PROCESS-RECEIPT-TRAN
               UNTIL ALL-RECORDS-PROCESSED.

       500-PROCESS-RECEIPT-TRAN.

           PERFORM 510-RETURN-RECEIPT-TRANSACTION.
           PERFORM 520-PROCESS-INVENTORY-MASTER
               UNTIL IM-ITEM-NO >= RT-ITEM-NO.
           IF     IM-ITEM-NO = HIGH-VALUE
              AND RT-ITEM-NO = HIGH-VALUE
               SET ALL-RECORDS-PROCESSED TO TRUE
           ELSE
               IF IM-ITEM-NO = RT-ITEM-NO
                   PERFORM 550-APPLY-RECEIPT-TRANSACTION
               ELSE
                   PERFORM 240-WRITE-ERROR-TRANSACTION.

       510-RETURN-RECEIPT-TRANSACTION.

           RETURN SORTWORK INTO RECEIPT-TRANSACTION
               AT END
                   MOVE HIGH-VALUE TO RT-ITEM-NO.

       520-PROCESS-INVENTORY-MASTER.

           IF FIRST-EXECUTION
               PERFORM 530-READ-OLD-MASTER
               MOVE "N" TO FIRST-EXECUTION-SWITCH
           ELSE
               PERFORM 540-WRITE-NEW-MASTER
               PERFORM 530-READ-OLD-MASTER.

       530-READ-OLD-MASTER.

           READ OLDMAST INTO INVENTORY-MASTER-RECORD
               AT END
                   MOVE HIGH-VALUE TO IM-ITEM-NO.

       540-WRITE-NEW-MASTER.

           WRITE NEW-MASTER-RECORD FROM INVENTORY-MASTER-RECORD.
           IF NOT NEWMAST-SUCCESSFUL
               DISPLAY "WRITE ERROR ON NEWMAST FOR ITEM NUMBER "
                   IM-ITEM-NO
               DISPLAY "FILE STATUS CODE IS " NEWMAST-FILE-STATUS
               SET ALL-RECORDS-PROCESSED TO TRUE.

       550-APPLY-RECEIPT-TRANSACTION.

           ADD RT-RECEIPT-QUANTITY TO IM-ON-HAND.
           SUBTRACT RT-RECEIPT-QUANTITY FROM IM-ON-ORDER.
