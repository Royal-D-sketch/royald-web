      var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle=""tooltip""]'))
      var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
          return new bootstrap.Tooltip(tooltipTriggerEl)
      });
  
      document.querySelectorAll('input[name=""method""]').forEach(r => {
          r.addEventListener('change', function () {
              document.getElementById('checkFields').classList.toggle('d-none', this.value !== 'Check');
              document.getElementById('uploadField').classList.toggle('d-none', this.value === 'Cash');
          });
      });
  
      const statusSelect = document.getElementById('statusSelect');
      statusSelect.addEventListener('change', function() {
          const val = this.value;
          document.getElementById('f-postponed').classList.add('d-none');
          document.getElementById('f-delivering').classList.add('d-none');
          document.getElementById('f-waiting').classList.add('d-none');
          document.getElementById('f-baddebt').classList.add('d-none');
          document.getElementById('f-return').classList.add('d-none');
          document.getElementById('f-file').classList.add('d-none');
  
          if (val === 'Postponed') document.getElementById('f-postponed').classList.remove('d-none');
          if (val === 'Delivering') document.getElementById('f-delivering').classList.remove('d-none');
          if (val === 'WaitingGoods') document.getElementById('f-waiting').classList.remove('d-none');
          if (val === 'BadDebt') { 
              document.getElementById('f-baddebt').classList.remove('d-none');
              document.getElementById('f-file').classList.remove('d-none');
          }
          if (val === 'ReturnIssued' || val === 'ReturnPending') {
              document.getElementById('f-return').classList.remove('d-none');
              document.getElementById('f-file').classList.remove('d-none');
              if (val === 'ReturnIssued') document.getElementById('ret2').click();
              else document.getElementById('ret1').click();
          }
          if (val === 'ChangeProduct') document.getElementById('f-file').classList.remove('d-none');
      });
  
      document.querySelectorAll('input[name=""returnType""]').forEach(r => {
          r.addEventListener('change', function() {
              if(this.value === 'Issued') document.getElementById('f-return-amount').classList.remove('d-none');
              else document.getElementById('f-return-amount').classList.add('d-none');
              statusSelect.value = this.value === 'Issued' ? 'ReturnIssued' : 'ReturnPending';
          });
      });
