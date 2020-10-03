#pragma once

using namespace System;
using namespace Stride;
using namespace System::Threading::Tasks;
namespace StrideCpp {
	public ref class Sync : Engine::SyncScript {
	public:
			void Start() override;
			void Update() override;
	};
	public ref class Async : Engine::AsyncScript {
	public:
		Task^ Execute() override;
	};
	public ref class Startup : Engine::StartupScript {
	public:
		void Start() override;
	};
}
